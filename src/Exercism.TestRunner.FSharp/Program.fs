module Exercism.TestRunner.FSharp.Program

open System
open System.IO
open CommandLine
open Humanizer
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Testing
open Exercism.TestRunner.FSharp.Output

type Options =
    { [<Value(0, Required = true, HelpText = "The solution's exercise")>]
      Slug: string
      [<Value(1, Required = true, HelpText = "The directory containing the solution")>]
      InputDirectory: string
      [<Value(2, Required = true, HelpText = "The directory to which the results will be written")>]
      OutputDirectory: string }

let private parseOptions argv =
    match Parser.Default.ParseArguments<Options>(argv) with
    | :? Parsed<Options> as options -> Some options.Value
    | _ -> None

let private createTestRunContext options =
    let exercise = options.Slug.Dehumanize().Pascalize()
    let (</>) left right = Path.Combine(left, right)

    { TestsFile =
          options.InputDirectory
          </> sprintf "%sTests.fs" exercise
      TestResultsFile =
          options.InputDirectory
          </> "TestResults"
          </> "tests.trx"
      BuildLogFile = options.InputDirectory </> "msbuild.log"
      ResultsFile = options.OutputDirectory </> "results.json" }

let private runTestRunner options =
    let currentDate () = DateTimeOffset.UtcNow.ToString("u")

    printfn "[%s] Running test runner for '%s' solution..." (currentDate ()) options.Slug

    let context = createTestRunContext options
    let testRun = runTests context
    writeTestResults context testRun

    printfn "[%s] Ran test runner for '%s' solution" (currentDate ()) options.Slug

[<EntryPoint>]
let main argv =
    match parseOptions argv with
    | Some options ->
        runTestRunner options
        0
    | None -> 1
