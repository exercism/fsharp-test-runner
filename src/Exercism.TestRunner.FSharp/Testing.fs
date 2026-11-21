module Exercism.TestRunner.FSharp.Testing

open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open System.Xml.Serialization
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Rewrite
open Exercism.TestRunner.FSharp.Visitor
open Fantomas.FCS.Syntax
open Fantomas.FCS.Text

module String =
    let normalize (str: string) = str.Replace("\r\n", "\n").Trim()

    let isNullOrWhiteSpace = System.String.IsNullOrWhiteSpace

module Process =
    let exec (fileName: string) (arguments: string) workingDirectory =
        let psi = ProcessStartInfo(fileName, arguments)
        psi.WorkingDirectory <- workingDirectory
        psi.RedirectStandardInput <- true
        psi.RedirectStandardError <- true
        psi.RedirectStandardOutput <- true
        use p = Process.Start(psi)
        // Read both streams to the end, so a child that writes a lot of output
        // can't fill the pipe and block.
        let stdout = p.StandardOutput.ReadToEndAsync()
        let stderr = p.StandardError.ReadToEndAsync()
        p.WaitForExit()
        stdout.Wait()
        stderr.Wait()

module TestResults =
    [<AllowNullLiteral>]
    [<XmlRoot(ElementName = "ErrorInfo", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
    type XmlErrorInfo() =
        [<XmlElement(ElementName = "Message", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
        member val Message: string = null with get, set

    [<AllowNullLiteral>]
    [<XmlRoot(ElementName = "Output", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
    type XmlOutput() =
        [<XmlElement(ElementName = "StdOut", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
        member val StdOut: string = null with get, set

        [<XmlElement(ElementName = "ErrorInfo", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
        member val ErrorInfo: XmlErrorInfo = null with get, set

    [<AllowNullLiteral>]
    [<XmlRoot(ElementName = "UnitTestResult", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
    type XmlUnitTestResult() =
        [<XmlElement(ElementName = "Output", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
        member val Output: XmlOutput = null with get, set

        [<XmlAttribute(AttributeName = "testName")>]
        member val TestName: string = null with get, set

        [<XmlAttribute(AttributeName = "outcome")>]
        member val Outcome: string = null with get, set

    [<AllowNullLiteral>]
    [<XmlRoot(ElementName = "Results", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
    type XmlResults() =
        [<XmlElement(ElementName = "UnitTestResult",
                     Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
        member val UnitTestResult: XmlUnitTestResult [] = null with get, set

    [<XmlRoot(ElementName = "TestRun", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
    type XmlTestRun() =
        [<XmlElement(ElementName = "Results", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")>]
        member val Results: XmlResults = null with get, set

    let truncate (str: string) =
        let maxLength = 500

        if str.Length > maxLength then
            $"%s{str.[0..maxLength]}\nOutput was truncated. Please limit to %d{maxLength} chars."
        else
            str

    let private testMethodName =
        let regex = Regex(@"^(?:.*?\.)?(.+?)(?:<.+>\(.+\)|\(\))?$", RegexOptions.Compiled)
        fun (xmlUnitTestResult: XmlUnitTestResult) ->
            regex.Match(xmlUnitTestResult.TestName).Groups.[1].Value
    
    let private toName (xmlUnitTestResult: XmlUnitTestResult) =
        testMethodName xmlUnitTestResult

    let private toStatus (xmlUnitTestResult: XmlUnitTestResult) =
        match xmlUnitTestResult.Outcome with
        | "Passed" -> TestStatus.Pass
        | "Failed" -> TestStatus.Fail
        | _ -> TestStatus.Error

    // The xUnit test executable escapes some characters in test names and failure
    // messages with a backslash, such as \" for a quote and \x01 for other control
    // characters. This turns them back into the original characters.
    let private escapedCharacter = Regex(@"\\(x[01][0-9a-f]|.)", RegexOptions.Compiled)

    let private unescape (message: string) =
        escapedCharacter.Replace(message, fun m ->
            match m.Groups.[1].Value with
            | "n" -> "\n"
            | "r" -> "\r"
            | "t" -> "\t"
            | "0" -> "\000"
            | "a" -> "\a"
            | "b" -> "\b"
            | "f" -> "\f"
            | "v" -> "\v"
            | "\\" | "\"" as c -> c
            | hex when hex.Length = 3 -> string (char (System.Convert.ToInt32(hex.[1..], 16)))
            | _ -> m.Value)

    let private toMessage (xmlUnitTestResult: XmlUnitTestResult) =
        let removeFsUnitExceptionFromMessage (message: string) =
            message.Replace(
                "FsUnit.Xunit+MatchException : Exception of type 'FsUnit.Xunit+MatchException' was thrown.",
                ""
            )

        xmlUnitTestResult.Output
        |> Option.ofObj
        |> Option.bind (fun output -> output.ErrorInfo |> Option.ofObj)
        |> Option.bind (fun errorInfo -> errorInfo.Message |> Option.ofObj)
        |> Option.map unescape
        |> Option.map removeFsUnitExceptionFromMessage
        |> Option.map String.normalize

    let private toOutput (xmlUnitTestResult: XmlUnitTestResult) =
        xmlUnitTestResult.Output
        |> Option.ofObj
        |> Option.bind (fun output -> output.StdOut |> Option.ofObj)
        |> Option.map String.normalize
        |> Option.map truncate
    
    let private findTestMethodBinding (originalTestTree: ParsedInput) (xmlUnitTestResult: XmlUnitTestResult) =
        let originalTestName = testMethodName xmlUnitTestResult
        let mutable testMethodBinding = None

        let visitor =
            { new SyntaxVisitor() with
                member this.VisitSynModuleDecl(moduleDecl) =
                    match moduleDecl with
                    | SynModuleDecl.Let
                        (_,
                         [ SynBinding(_,
                                    _,
                                    _,
                                    _,
                                    _,
                                    _,
                                    _,
                                    SynPat.LongIdent (SynLongIdent (id, _, _), _, _, _, _, _),
                                    _,
                                    _,
                                    _,
                                    _,
                                    _) as binding ],
                         _) when id.[0].idText = originalTestName ->
                        testMethodBinding <- Some binding
                    | _ -> ()

                    base.VisitSynModuleDecl(moduleDecl)

                member this.VisitSynMemberDefn(memberDefn) =
                    match memberDefn with
                    | SynMemberDefn.Member
                        ( SynBinding (_,
                                              _,
                                              _,
                                              _,
                                              _,
                                              _,
                                              _,
                                              SynPat.LongIdent (SynLongIdent (id, _, _), _, _, _, _, _),
                                              _,
                                              _,
                                              _,
                                              _,
                                              _) as binding,
                                              _) when id.Length > 1 && id.[1].idText = originalTestName ->
                        testMethodBinding <- Some binding
                    | _ -> ()

                    base.VisitSynMemberDefn(memberDefn) }

        visitor.VisitInput(originalTestTree) |> ignore
        
        // Use .Value is safe here as we are guaranteed the test method exists
        testMethodBinding.Value 

    let private toTestCode (originalTestCode: ISourceText) ((SynBinding (_, _, _, _, _, _, _, _, _, expr, _, _, _)): SynBinding) =
        let range = expr.Range 
        
        if range.StartLine = range.EndLine then
            originalTestCode.GetLineString(range.StartLine - 1).[range.StartColumn - 1..range.EndColumn - 1].Trim()
        else
            [ range.StartLine .. range.EndLine ]
            |> List.map (fun line -> originalTestCode.GetLineString(line - 1).[range.StartColumn..])
            |> String.concat "\n"
        
    let private toTaskId ((SynBinding (_, _, _, _, attrs, _, _, _, _, _, _, _, _)): SynBinding) =           
        attrs
        |> Seq.collect (fun attrList -> attrList.Attributes)
        |> Seq.tryPick (fun attr ->
            match attr.TypeName with
            | (SynLongIdent (id, _, _)) when (id.ToString()) = "[Task]" ->
                match attr.ArgExpr with
                | SynExpr.Paren(SynExpr.Const(SynConst.Int32(i), _), _, _, _) -> Some (int i)
                | _ -> None
            | _ -> None)
        
    let private toLine ((SynBinding (_, _, _, _, _, _, _, _, _, _, range, _, _)): SynBinding) = range.StartLine

    let private toTestResult originalTestCode originalTestTree (xmlUnitTestResult: XmlUnitTestResult) =
        let testMethodBinding = findTestMethodBinding originalTestTree xmlUnitTestResult
        
        { Name = xmlUnitTestResult |> toName
          Status = xmlUnitTestResult |> toStatus
          Message = xmlUnitTestResult |> toMessage
          Output = xmlUnitTestResult |> toOutput
          TaskId = testMethodBinding |> toTaskId
          TestCode = testMethodBinding |> (toTestCode originalTestCode)
          Line = testMethodBinding |> toLine }

    let private toTestResults originalTestCode originalTestTree (xmlUnitTestResults: XmlUnitTestResult []) =
        for xmlUnitTestResult in xmlUnitTestResults do
            xmlUnitTestResult.TestName <- unescape xmlUnitTestResult.TestName

        xmlUnitTestResults
        |> Seq.map (toTestResult originalTestCode originalTestTree)
        |> Seq.sortBy (fun testResult -> testResult.Line)
        |> Seq.toArray

    let parse originalTestCode originalTestTree context =
        use fileStream = File.OpenRead(context.TestResultsFile)

        let result =
            XmlSerializer(typeof<XmlTestRun>)
                .Deserialize(fileStream)
            :?> XmlTestRun

        result.Results
        |> Option.ofObj
        |> Option.bind (fun results -> results.UnitTestResult |> Option.ofObj)
        |> Option.map (toTestResults originalTestCode originalTestTree)
        |> Option.defaultValue Array.empty

module DotnetCli =
    type DotnetTestResult =
        | TestRunSuccess of TestResult []
        | TestRunError of string []

    let private removePaths (error: string) =
        let testsFsIndex = error.IndexOf("Tests.fs")

        if testsFsIndex = -1 then
            error
        else
            let lastPathIndex =
                error.LastIndexOf(Path.DirectorySeparatorChar, testsFsIndex)

            if lastPathIndex = -1 then
                error
            else
                error.[lastPathIndex + 1..]

    let private removeProjectReference (error: string) = error[.. error.LastIndexOf('[') - 1]

    let private normalizeBuildError error =
        error
        |> removeProjectReference
        |> removePaths
        |> String.normalize

    let private toBuildErrors lines =
        lines
        |> Seq.map normalizeBuildError
        |> Seq.filter (fun logLine -> logLine |> String.isNullOrWhiteSpace |> not)
        |> Seq.toArray

    // Every exercise uses xUnit v3, so the project builds to an executable
    // that runs the tests itself. Running it directly skips `dotnet test`,
    // which spends about 3 s starting MSBuild and the VSTest host for every run.
    let private runTestExecutable (assembly: string) originalTestCode originalTestTree context =
        Process.exec "dotnet" $"\"{assembly}\" -noLogo -noColor -trx \"{context.TestResultsFile}\"" (Path.GetDirectoryName(context.TestsFile))

        try
            TestRunSuccess(TestResults.parse originalTestCode originalTestTree context)
        with error ->
            // For example, the test executable crashed before writing its results.
            TestRunError [| error.Message |]

    let private runTestsWithMSBuild originalTestCode originalTestTree context =
        let solutionDir = Path.GetDirectoryName(context.TestsFile)

        Process.exec "dotnet" "restore --source /root/.nuget/packages/" solutionDir
        Process.exec "dotnet" "build --configuration release --no-restore --verbosity quiet /flp:verbosity=quiet;errorsOnly=true" solutionDir

        let buildErrors = toBuildErrors (File.ReadLines(context.BuildLogFile))

        if Array.isEmpty buildErrors then
            let assembly =
                Path.Combine(solutionDir, "bin", "release", "net10.0", Path.GetFileNameWithoutExtension(context.ProjectFile) + ".dll")

            runTestExecutable assembly originalTestCode originalTestTree context
        else
            TestRunError buildErrors

    let runTests originalTestCode originalTestTree context =
        match FastPath.compile context with
        | FastPath.Compiled assembly -> runTestExecutable assembly originalTestCode originalTestTree context
        | FastPath.CompileErrors errors -> TestRunError(toBuildErrors errors)
        | FastPath.NotUsed reason ->
            printfn $"Fast path not used: %s{reason}"
            runTestsWithMSBuild originalTestCode originalTestTree context

let toTestStatus (testResults: TestResult []) =
    let testStatuses =
        testResults
        |> Seq.map (fun testResult -> testResult.Status)
        |> Set.ofSeq

    if testStatuses = Set.singleton TestStatus.Pass then
        TestStatus.Pass
    elif Set.contains TestStatus.Fail testStatuses then
        TestStatus.Fail
    else
        TestStatus.Error

let private testRunFromTestRunnerSuccess testResults =
    { Message = None
      Status = testResults |> toTestStatus
      Tests = testResults }

let private testRunFromTestRunnerError errors =
    { Message = errors |> String.concat "\n" |> Some
      Status = TestStatus.Error
      Tests = Array.empty }

let runTests context =
    match rewriteTests context with
    | RewriteSuccess (originalTestCode, originalTestTree, rewrittenTestCode, originalProjectFile, rewrittenProjectFile) ->
        try
            File.WriteAllText(context.TestsFile, rewrittenTestCode.ToString())
            File.WriteAllText(context.ProjectFile, rewrittenProjectFile.ToString())

            match DotnetCli.runTests originalTestCode originalTestTree context with
            | DotnetCli.TestRunSuccess testResults -> testRunFromTestRunnerSuccess testResults
            | DotnetCli.TestRunError errors -> testRunFromTestRunnerError errors
        finally
            File.WriteAllText(context.TestsFile, originalTestCode.ToString())
            File.WriteAllText(context.ProjectFile, originalProjectFile.ToString())
    | RewriteError -> testRunFromTestRunnerError [ "Could not modify test suite" ]
