module Exercism.TestRunner.FSharp.Testing

open System.Diagnostics
open System.IO
open System.Xml.Serialization
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Rewrite

module String =
    let normalize (str: string) = str.Replace("\r\n", "\n").Trim()

module Process =
    type ProcessResult =
        | ProcessSuccess
        | ProcessError
    
    let exec fileName arguments workingDirectory =
        let psi = ProcessStartInfo(fileName, arguments)
        psi.WorkingDirectory <- workingDirectory
        psi.RedirectStandardInput <- true
        psi.RedirectStandardError <- true
        psi.RedirectStandardOutput <- true
        use p = Process.Start(psi)
        p.WaitForExit()

        if p.ExitCode = 0 then ProcessSuccess else ProcessError

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

        if str.Length > maxLength
        then sprintf "%s\nOutput was truncated. Please limit to %d chars." str.[0..maxLength] maxLength
        else str

    let private toName (xmlUnitTestResult: XmlUnitTestResult) = xmlUnitTestResult.TestName
    
    let private toStatus (xmlUnitTestResult: XmlUnitTestResult) =
        match xmlUnitTestResult.Outcome with
        | "Passed" -> TestStatus.Pass
        | "Failed" -> TestStatus.Fail
        | _ -> TestStatus.Error

    let private toMessage (xmlUnitTestResult: XmlUnitTestResult) =
        xmlUnitTestResult.Output
        |> Option.ofObj
        |> Option.bind (fun output -> output.ErrorInfo |> Option.ofObj)
        |> Option.bind (fun errorInfo -> errorInfo.Message |> Option.ofObj)
        |> Option.map String.normalize

    let private toOutput (xmlUnitTestResult: XmlUnitTestResult) =
        xmlUnitTestResult.Output
        |> Option.ofObj
        |> Option.bind (fun output -> output.StdOut |> Option.ofObj)
        |> Option.map String.normalize
        |> Option.map truncate

    let private toTestResult (xmlUnitTestResult: XmlUnitTestResult) =
        {
            Name = xmlUnitTestResult |> toName
            Status = xmlUnitTestResult |> toStatus
            Message = xmlUnitTestResult |> toMessage
            Output = xmlUnitTestResult |> toOutput
        }
        
    let private toTestResults xmlUnitTestResults =
        xmlUnitTestResults
        |> Seq.map toTestResult
        |> Seq.sortBy (fun testResult -> testResult.Name)
        |> Seq.toArray
        
    let parse context =        
        use fileStream = File.OpenRead(context.TestResultsFile)
        let result = XmlSerializer(typeof<XmlTestRun>).Deserialize(fileStream) :?> XmlTestRun

        result.Results
        |> Option.ofObj
        |> Option.bind (fun results -> results.UnitTestResult |> Option.ofObj)
        |> Option.map toTestResults
        |> Option.defaultValue Array.empty

module DotnetCli =    
    type DotnetTestResult =
        | TestRunSuccess of TestResult[]
        | TestRunError of string[]
        
    let private removeProjectReference (error: string) =
        error.[0..(error.LastIndexOf('[') - 1)]
     
    let private normalizeBuildError error =
        error |> removeProjectReference |> String.normalize
 
    let private parseBuildErrors context =
        File.ReadLines(context.BuildLogFile)
        |> Seq.map normalizeBuildError
        |> Seq.toArray
        
    let private parseTestResults context =
        TestResults.parse context
    
    let runTests context =
        let command = "dotnet"
        let arguments = sprintf "test --verbosity=quiet --logger \"trx;LogFileName=%s\" /flp:v=q" (Path.GetFileName(context.BuildLogFile))
        
         
        
        match Process.exec command arguments (Path.GetDirectoryName(context.TestsFile)) with
        | Process.ProcessSuccess -> TestRunSuccess (parseTestResults context)
        | Process.ProcessError -> TestRunError (parseBuildErrors context)

//let private failureToMessage messages =
//    messages
//    |> Array.map String.normalize
//    |> Array.map (fun message ->
//        message.Replace("Exception of type 'FsUnit.Xunit+MatchException' was thrown.\n", "").Trim())
//    |> String.concat "\n"

let toTestStatus (testResults: TestResult[]) =
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

let private testResultsFromDotnetTest context =
    match DotnetCli.runTests context with
    | DotnetCli.TestRunSuccess testResults -> testRunFromTestRunnerSuccess testResults
    | DotnetCli.TestRunError errors -> testRunFromTestRunnerError errors       

let runTests context =
    match rewriteTests context with
    | RewriteSuccess (originalTestCode, rewrittenTestCode) ->
        try
            File.WriteAllText(context.TestsFile, rewrittenTestCode)            
            testResultsFromDotnetTest context
        finally
            File.WriteAllText(context.TestsFile, originalTestCode)
    | RewriteError ->
        testRunFromTestRunnerError ["Could not modify test suite"]
