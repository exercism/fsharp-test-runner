module Exercism.TestRunner.FSharp.Program

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Runtime.Loader
open System.Threading
//open CommandLine
open FSharp.Compiler.SourceCodeServices
open Xunit.Runners
//open Humanizer
//open Exercism.TestRunner.FSharp.Core
//open Exercism.TestRunner.FSharp.Testing
//open Exercism.TestRunner.FSharp.Output
//open Xunit.Runners

//type Options =
//    { [<Value(0, Required = true, HelpText = "The solution's exercise")>]
//      Slug: string
//      [<Value(1, Required = true, HelpText = "The directory containing the solution")>]
//      InputDirectory: string
//      [<Value(2, Required = true, HelpText = "The directory to which the results will be written")>]
//      OutputDirectory: string }
//
//let private parseOptions argv =
//    match Parser.Default.ParseArguments<Options>(argv) with
//    | :? (Parsed<Options>) as options -> Some options.Value
//    | _ -> None
//
//let private createTestRunContext options =
//    let exercise = options.Slug.Dehumanize().Pascalize()
//    let (</>) left right = Path.Combine(left, right)
//
//    { TestsFile = options.InputDirectory </> $"%s{exercise}Tests.fs"
//      TestResultsFile =
//          options.InputDirectory
//          </> "TestResults"
//          </> "tests.trx"
//      BuildLogFile = options.InputDirectory </> "msbuild.log"
//      ResultsFile = options.OutputDirectory </> "results.json" }
//
//let private runTestRunner options =
//    let currentDate () = DateTimeOffset.UtcNow.ToString("u")
//
//    printfn $"[%s{currentDate ()}] Running test runner for '%s{options.Slug}' solution..."
//
//    let context = createTestRunContext options
//    let testRun = runTests context
//    writeTestResults context testRun
//
//    printfn $"[%s{currentDate ()}] Ran test runner for '%s{options.Slug}' solution"

let checker = FSharpChecker.Create()

let projectArgs =
    let dllName = "MultipleTestsWithSingleFail.dll"
    let projectFileName = "Fake.fsproj" 
    let fileName1 = "/Users/erik/Code/exercism/fsharp-test-runner/test/Exercism.TestRunner.FSharp.IntegrationTests/Solutions/MultipleTestsWithMultipleFails/Fake.fs"
    let fileName2 = "/Users/erik/Code/exercism/fsharp-test-runner/test/Exercism.TestRunner.FSharp.IntegrationTests/Solutions/MultipleTestsWithMultipleFails/FakeTests.fs"
    let dir = Directory.GetCurrentDirectory()
    let references =
        AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES").ToString().Split(Path.PathSeparator)
        
    references |> Seq.filter (fun a -> a.Contains("xunit") || a.Contains("crest")) |> Seq.iter (printfn "%A")
    
    [| yield "--simpleresolution"
       yield "--noframework"
       yield "--debug-"
       yield "--define:DEBUG"
       yield "--optimize-"
       yield "--out:" + dllName
       yield "--fullpaths"
       yield "--target:library"
       yield fileName1
       yield fileName2
       for r in references do
             yield "-r:" + r |]

let projectOptions =
    // TODO: read files from .meta/config.json
    let dllName = "MultipleTestsWithSingleFail.dll"
    let projectFileName = "Fake.fsproj" 
    let fileName1 = "/Users/erik/Code/exercism/fsharp-test-runner/test/Exercism.TestRunner.FSharp.IntegrationTests/Solutions/MultipleTestsWithMultipleFails/Fake.fs"
    let fileName2 = "/Users/erik/Code/exercism/fsharp-test-runner/test/Exercism.TestRunner.FSharp.IntegrationTests/Solutions/MultipleTestsWithMultipleFails/FakeTests.fs"
    let dir = Directory.GetCurrentDirectory()
    let references =
        AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES").ToString().Split(Path.PathSeparator)
    
    checker.GetProjectOptionsFromCommandLineArgs(projectFileName, projectArgs)

[<EntryPoint>]
let main argv =
    
    let toolsPath = Ionide.ProjInfo.Init.init()
    let loader = Ionide.ProjInfo.WorkspaceLoader.Create(toolsPath, [])
    let projs = loader.LoadProjects(["/Users/erik/Code/exercism/fsharp-test-runner/test/Exercism.TestRunner.FSharp.IntegrationTests/Solutions/MultipleTestsWithSingleFail/Fake.fsproj"])
    let proj = Seq.head projs
    
    
    let x = Ionide.ProjInfo.FCS.mapToFSharpProjectOptions proj projs
//    let (a, b) = checker.GetParsingOptionsFromProjectOptions(x)
    let e = checker.ParseAndCheckProject(x) |> Async.RunSynchronously
//    let args = Ionide.ProjInfo.ProjectLoader.getFscArgs(x)

    // TODO: use parsed input files
    let (errors, result) = checker.Compile(projectArgs) |> Async.RunSynchronously
    [ for error in errors -> printfn "%A" error ]
    
//    let references = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES").ToString().Split(Path.PathSeparator)
//    for ref in references do
//        Assembly.LoadFrom(ref)

//    for ass in AssemblyLoadContext.Default.Assemblies do
//        printfn $"{ass.FullName}"
    
//    AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/MultipleTestsWithSingleFail.dll")
//    AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/NHamcrest.dll")
//    AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/FsUnit.Xunit.dll")
//    AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/xunit.abstractions.dll")
//    AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/xunit.assert.dll")
//    AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/xunit.core.dll")
//    AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/xunit.execution.dotnet.dll")
//    AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/xunit.runner.utility.netcoreapp10.dll")
    
//    for ass in AssemblyLoadContext.Default.Assemblies do
//        printfn $"{ass.FullName}: {ass.Location}"
    
//    Assembly.LoadFrom("/Users/erik/.nuget/packages/nhamcrest/2.0.1/lib/netstandard1.5/NHamcrest.dll") |> ignore
//    Assembly.LoadFile("/Users/erik/Code/exercism/fsharp-test-runner/src/Exercism.TestRunner.FSharp/temp/MultipleTestsWithSingleFail.dll") |> ignore

    let tests = ConcurrentStack<TestInfo>()
    let finished = new ManualResetEventSlim()
    let runner = AssemblyRunner.WithoutAppDomain("MultipleTestsWithSingleFail.dll")
    runner.OnTestFailed <- Action<TestFailedInfo>(tests.Push)
    runner.OnTestPassed <- Action<TestPassedInfo>(tests.Push)
    runner.OnExecutionComplete <- Action<ExecutionCompleteInfo>(fun info -> finished.Set())

    runner.Start()
    finished.Wait()
    
    for test in tests do
        printfn "%s" (test.GetType().Name)
    
//    let wholeProjectResults = checker.ParseAndCheckProject(projectOptions) |> Async.RunSynchronously
//    [ for error in wholeProjectResults.Errors -> printfn "%A" error ]
//    [ for x in wholeProjectResults.AssemblySignature.Entities -> printfn "%A" x.DisplayName ]
    
    0
//    match parseOptions argv with
//    | Some options ->
//        runTestRunner options
//        0
//    | None -> 1
