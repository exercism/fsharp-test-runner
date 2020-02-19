module Exercism.TestRunner.FSharp.Project

open Dotnet.ProjInfo.Workspace
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Utils
open System.IO
open System.Reflection
open FSharp.Compiler.SourceCodeServices

type ProjectCompilationError =
    | ProjectNotFound
    | TestFileNotFound
    | CompilationFailed
    | CompilationError of FSharpErrorInfo[]

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
        Process.exec "dotnet" "restore" (Path.GetDirectoryName projectFile)
        |> Result.mapError (fun _ -> CompilationFailed)
        
let private getCompileOptions (context: TestRunContext) =
    let addCompileOptions (projectOptions: FCS.FCS_ProjectOptions) =
        projectOptions.SourceFiles
        |> Array.collect (fun x -> [| "-a"; x |])
        |> Array.append projectOptions.OtherOptions
        |> Array.append [| "fcs.exe" |]
        
    let getProjectOptions () =
        loader.LoadProjects [context.ProjectFile] |> ignore
        binder.GetProjectOptions(context.ProjectFile)
        |> Result.mapError (fun _ -> CompilationFailed)
    
    dotnetRestore context.ProjectFile
    |> Result.bind getProjectOptions
    |> Result.map addCompileOptions
    
let private assemblyFilePath (compileOptions: string[]) =
    let outputCompileOption =
        compileOptions
        |> Array.find (fun compileOption -> compileOption.StartsWith("-o:"))

    outputCompileOption.[3..]
    
let private enableAllTests (context: TestRunContext) =
    if File.Exists(context.TestFile) then
        File.regexReplace context.TestFile "\(\s*Skip\s*=\s*\"Remove to run test\"\s*\)" ""
        Result.Ok context
    else
        Result.Error TestFileNotFound
    
let private compile (context: TestRunContext) =
    let compileFromOptions compileOptions =
        let errors, exitCode =
            checker.Compile(compileOptions)
            |> Async.RunSynchronously

        if exitCode = 0 then
            Result.Ok (Assembly.LoadFile(assemblyFilePath compileOptions))
        else
            // TODO: check if only errors are returned (filter on severity)
            Result.Error (CompilationError errors)
    
    getCompileOptions context
    |> Result.bind compileFromOptions 

let compileProject (context: TestRunContext) =
    context
    |> enableAllTests
    |> Result.bind compile