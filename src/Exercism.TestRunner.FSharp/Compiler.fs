module Exercism.TestRunner.FSharp.Compiler

open Dotnet.ProjInfo.Workspace
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Visitor
open Exercism.TestRunner.FSharp.Utils
open FSharp.Compiler.Ast
open System
open System.IO
open System.Reflection
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Text
open Fantomas

type CompilerError =
    | ProjectNotFound
    | TestsFileNotFound
    | CompilationFailed
    | CompilationError of FSharpErrorInfo []

type TestModuleSimplifier() =
    inherit SyntaxVisitor()

    override this.VisitSynAttribute(attr: SynAttribute): SynAttribute =
        match attr.TypeName with
        | LongIdentWithDots([ ident ], _) when ident.idText = "Fact" ->
            base.VisitSynAttribute({ attr with ArgExpr = SynExpr.Const(SynConst.Unit, attr.ArgExpr.Range) })
        | _ -> base.VisitSynAttribute(attr)

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
    
let private treeToCode tree = CodeFormatter.FormatASTAsync(tree, "", [], None, FormatConfig.FormatConfig. Default) |> Async.RunSynchronously

let private enableAllTests (context: TestRunContext) parsedInput =
    let visited = TestModuleSimplifier().VisitInput(parsedInput)
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
        let errors, exitCode = checker.Compile(compileOptions) |> Async.RunSynchronously

        if exitCode = 0
        then Result.Ok(Assembly.LoadFile(assemblyFilePath compileOptions))
        else Result.Error(CompilationError errors)

    getCompileOptions projectOptions |> compileFromOptions

let compileProject (context: TestRunContext) =
    getProjectOptions context
    |> Result.bind (rewriteSyntax context)
    |> Result.bind compile
