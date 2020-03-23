module Exercism.TestRunner.FSharp.Compiler

open Dotnet.ProjInfo.Workspace
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Visitor
open FSharp.Compiler.Ast
open System
open System.IO
open System.Reflection
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Text
open Fantomas

module Process =
    let exec fileName arguments workingDirectory =
        let psi = System.Diagnostics.ProcessStartInfo()
        psi.FileName <- fileName
        psi.Arguments <- arguments
        psi.WorkingDirectory <- workingDirectory
        psi.CreateNoWindow <- true
        psi.UseShellExecute <- false

        use p = new System.Diagnostics.Process()
        p.StartInfo <- psi

        p.Start() |> ignore
        p.WaitForExit()

        if p.ExitCode = 0 then Result.Ok()
        else Result.Error()

type CompilerError =
    | ProjectNotFound
    | TestsFileNotFound
    | CompilationFailed
    | CompilationError of FSharpErrorInfo []

type EnableAllTests() =
    inherit SyntaxVisitor()

    override this.VisitSynAttribute(attr: SynAttribute): SynAttribute =
        match attr.TypeName with
        | LongIdentWithDots([ ident ], _) when ident.idText = "Fact" ->
            base.VisitSynAttribute({ attr with ArgExpr = SynExpr.Const(SynConst.Unit, attr.ArgExpr.Range) })
        | _ -> base.VisitSynAttribute(attr)

type CaptureConsoleOutput() =
    inherit SyntaxVisitor()

    override this.VisitSynModuleOrNamespace(modOrNs: SynModuleOrNamespace): SynModuleOrNamespace =
        match modOrNs with
        | SynModuleOrNamespace(longIdent, isRecursive, isModule, decls, doc, attrs, access, range) ->
            let (letDecls, otherDecls) =
                decls
                |> List.partition (function
                    | SynModuleDecl.Let(_, _, _) -> true
                    | _ -> false)

            let letDeclBindings =
                letDecls
                |> List.choose (function
                    | SynModuleDecl.Let(isRecursive, bindings, _) ->
                        Some(SynMemberDefn.LetBindings(bindings, false, isRecursive, range))
                    | _ -> None)

            let ident name = ident (name, range)

            let longIdentWithDots names =
                LongIdentWithDots.LongIdentWithDots(names |> List.map (fun name -> ident name), [])

            let constructor =
                SynMemberDefn.ImplicitCtor
                    (None, [],
                     SynSimplePats.SimplePats
                         ([ SynSimplePat.Typed
                             (SynSimplePat.Id(ident "testOutput", None, false, false, false, range),
                              SynType.LongIdent(longIdentWithDots [ "Xunit"; "Abstractions"; "ITestOutputHelper" ]),
                              range) ], range), None, range)

            let stringWriter =
                SynMemberDefn.LetBindings
                    ([ SynBinding.Binding
                        (None, SynBindingKind.NormalBinding, false, false, [], PreXmlDoc.Empty,
                         SynValData(None, SynValInfo([], SynArgInfo([], false, None)), None),
                         SynPat.Named(SynPat.Wild(range), ident "stringWriter", false, None, range), None,
                         SynExpr.New
                             (false, SynType.LongIdent(longIdentWithDots [ "System"; "IO"; "StringWriter" ]),
                              SynExpr.Const(SynConst.Unit, range), range), range,
                         SequencePointInfoForBinding.NoSequencePointAtLetBinding) ], false, false, range)

            let captureOutput =
                SynMemberDefn.LetBindings
                    ([ SynBinding.Binding
                        (None, SynBindingKind.DoBinding, false, false, [], PreXmlDoc.Empty,
                         SynValData(None, SynValInfo([], SynArgInfo([], false, None)), None),
                         SynPat.Const(SynConst.Unit, range), None,
                         SynExpr.Sequential
                             (SequencePointInfoForSeq.SequencePointsAtSeq, true,
                              SynExpr.App
                                  (ExprAtomicFlag.Atomic, false,
                                   SynExpr.LongIdent
                                       (false, longIdentWithDots [ "System"; "Console"; "SetOut" ], None, range),
                                   SynExpr.Paren(SynExpr.Ident(ident "stringWriter"), range, None, range), range),
                              SynExpr.Sequential
                                  (SequencePointInfoForSeq.SequencePointsAtSeq, true,
                                   SynExpr.App
                                       (ExprAtomicFlag.Atomic, false,
                                        SynExpr.LongIdent
                                            (false, longIdentWithDots [ "System"; "Console"; "SetError" ], None, range),
                                        SynExpr.Paren(SynExpr.Ident(ident "stringWriter"), range, None, range), range),
                                   SynExpr.App
                                       (ExprAtomicFlag.NonAtomic, false,
                                        SynExpr.App
                                            (ExprAtomicFlag.NonAtomic, true, SynExpr.Ident(ident "op_PipeRight"),
                                             SynExpr.App
                                                 (ExprAtomicFlag.Atomic, false,
                                                  SynExpr.LongIdent
                                                      (false,
                                                       longIdentWithDots
                                                           [ "System"; "Diagnostics"; "Trace"; "Listeners"; "Add" ],
                                                       None, range),
                                                  SynExpr.Paren
                                                      (SynExpr.New
                                                          (false,
                                                           SynType.LongIdent
                                                               (longIdentWithDots
                                                                   [ "System"; "Diagnostics"; "ConsoleTraceListener" ]),
                                                           SynExpr.Const(SynConst.Unit, range), range), range, None,
                                                       range), range), range), SynExpr.Ident(ident "ignore"), range),
                                   range), range), range, SequencePointInfoForBinding.NoSequencePointAtDoBinding) ],
                     false, false, range)

            let interfaceImpl =
                SynMemberDefn.Interface
                    (SynType.LongIdent(longIdentWithDots [ "System"; "IDisposable" ]),
                     Some
                         ([ SynMemberDefn.Member(SynBinding.Binding
                                                     (None, SynBindingKind.NormalBinding, false, false, [],
                                                      PreXmlDoc.Empty,
                                                      SynValData.SynValData
                                                          (Some
                                                              ({ IsInstance = true
                                                                 IsDispatchSlot = false
                                                                 IsOverrideOrExplicitImpl = true
                                                                 IsFinal = false
                                                                 MemberKind = MemberKind.Member }),
                                                           SynValInfo([], SynArgInfo([], false, None)), None),
                                                      SynPat.LongIdent
                                                          (longIdentWithDots [ "__"; "Dispose" ], None, None,
                                                           SynConstructorArgs.Pats
                                                               ([ SynPat.Paren
                                                                   (SynPat.Const(SynConst.Unit, range), range) ]), None,
                                                           range), None,
                                                      SynExpr.App
                                                          (ExprAtomicFlag.Atomic, false,
                                                           SynExpr.LongIdent
                                                               (false, longIdentWithDots [ "testOutput"; "WriteLine" ],
                                                                None, range),
                                                           SynExpr.Paren
                                                               (SynExpr.App
                                                                   (ExprAtomicFlag.Atomic, false,
                                                                    SynExpr.LongIdent
                                                                        (false,
                                                                         longIdentWithDots
                                                                             [ "stringWriter"; "ToString" ], None, range),
                                                                    SynExpr.Const(SynConst.Unit, range), range), range,
                                                                None, range

                                                               ), range), range,
                                                      SequencePointInfoForBinding.NoSequencePointAtInvisibleBinding),
                                                 range) ]), range)

            let newDecls =
                otherDecls
                @ [ SynModuleDecl.Types
                        ([ TypeDefn
                            (ComponentInfo([], [], [], [ ident "Tests" ], doc, false, None, range),
                             SynTypeDefnRepr.ObjectModel
                                 (SynTypeDefnKind.TyconClass,
                                  letDeclBindings @ [ constructor; stringWriter; captureOutput; interfaceImpl ], range),
                             [], range) ], range) ]
            SynModuleOrNamespace
                (this.VisitLongIdent longIdent, isRecursive, isModule, newDecls |> List.map this.VisitSynModuleDecl, doc,
                 attrs |> List.map this.VisitSynAttributeList, Option.map this.VisitSynAccess access, range)

let private checker = FSharpChecker.Create()

let private msBuildLocator = MSBuildLocator()

let private loaderConfig = Dotnet.ProjInfo.Workspace.LoaderConfig.Default msBuildLocator
let private loader = Dotnet.ProjInfo.Workspace.Loader.Create(loaderConfig)
let private infoConfig = Dotnet.ProjInfo.Workspace.NetFWInfoConfig.Default msBuildLocator
let private netFwInfo = Dotnet.ProjInfo.Workspace.NetFWInfo.Create(infoConfig)
let private binder = Dotnet.ProjInfo.Workspace.FCS.FCSBinder(netFwInfo, loader, checker)

let private dotnetRestore (projectFile: string) =
    if not (File.Exists projectFile) then
        Result.Error ProjectNotFound
    else
        let homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        let nugetDir = Path.Combine(homeDir, ".nuget")
        let arguments = sprintf "restore -s %s" nugetDir
        let workingDir = Path.GetDirectoryName projectFile
        Process.exec "dotnet" arguments workingDir |> Result.mapError (fun _ -> CompilationFailed)

let getProjectOptions (context: TestRunContext) =
    dotnetRestore context.ProjectFile
    |> Result.bind (fun _ ->
        loader.LoadProjects [ context.ProjectFile ] |> ignore
        binder.GetProjectOptions(context.ProjectFile) |> Result.mapError (fun _ -> CompilationFailed))

let getParseOptions (projectOptions: FCS.FCS_ProjectOptions) =
    match checker.GetParsingOptionsFromProjectOptions(projectOptions) with
    | parseOptions, [] -> Result.Ok parseOptions
    | _, _ -> Result.Error CompilationFailed

let private getCompileOptions (projectOptions: FCS.FCS_ProjectOptions) =
    projectOptions.SourceFiles
    |> Array.collect (fun x -> [| "-a"; x |])
    |> Array.append projectOptions.OtherOptions
    |> Array.append [| "fcs.exe" |]

let private assemblyFilePath (compileOptions: string []) =
    let outputCompileOption = compileOptions |> Array.find (fun compileOption -> compileOption.StartsWith("-o:"))

    outputCompileOption.[3..]

let private parseFile (filePath: string) (parseOptions: FSharpParsingOptions) =
    let parsedResult =
        checker.ParseFile(filePath, File.ReadAllText(filePath) |> SourceText.ofString, parseOptions)
        |> Async.RunSynchronously

    match parsedResult.ParseTree with
    | Some tree -> Result.Ok tree
    | None -> Result.Error CompilationFailed

let private treeToCode tree =
    CodeFormatter.FormatASTAsync(tree, "", [], None, FormatConfig.FormatConfig.Default) |> Async.RunSynchronously

let private enableAllTests (context: TestRunContext) parsedInput =
    let visitors: SyntaxVisitor list =
        [ EnableAllTests()
          CaptureConsoleOutput() ]

    let visited = visitors |> List.fold (fun tree visitor -> visitor.VisitInput(tree)) parsedInput

    let code = treeToCode visited
    File.WriteAllText(context.TestsFile, code)

let private rewriteSyntax (context: TestRunContext) (projectOptions: FCS.FCS_ProjectOptions) =
    if File.Exists(context.TestsFile) then
        getParseOptions projectOptions
        |> Result.bind (parseFile context.TestsFile)
        |> Result.map (enableAllTests context)
        |> Result.map (fun _ -> projectOptions)
    else
        Result.Error TestsFileNotFound

let private compile (projectOptions: FCS.FCS_ProjectOptions) =
    let compileFromOptions compileOptions =
        let errors, _ = checker.Compile(compileOptions) |> Async.RunSynchronously
        let nonWarningErrors = errors |> Array.filter (fun error -> error.Severity = FSharpErrorSeverity.Error)

        if Array.isEmpty nonWarningErrors then Result.Ok(Assembly.LoadFile(assemblyFilePath compileOptions))
        else Result.Error(CompilationError nonWarningErrors)

    getCompileOptions projectOptions |> compileFromOptions

let compileProject (context: TestRunContext) =
    getProjectOptions context
    |> Result.bind (rewriteSyntax context)
    |> Result.bind compile
