module Exercism.TestRunner.FSharp.Program

open System.IO
open CommandLine
open Humanizer
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Testing
open Exercism.TestRunner.FSharp.Compiler
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
    | :? (Parsed<Options>) as options -> Some options.Value
    | _ -> None

let private createTestRunContext options =
    let exercise = options.Slug.Dehumanize().Pascalize()

    let testsFile = Path.Combine(options.InputDirectory, sprintf "%sTests.fs" exercise)
    let testResultsFile = Path.Combine(options.InputDirectory, "msbuild.log")
    let buildLogFile = Path.Combine(options.InputDirectory, "TestResults", "tests.trx")
    let resultsFile = Path.Combine(options.OutputDirectory, "results.json")

    { TestsFile = testsFile
      TestResultsFile = testResultsFile
      BuildLogFile = buildLogFile
      ResultsFile = resultsFile }

let private parseSuccess options =
    let context = createTestRunContext options
    let testRun = runTests context
    writeTestResults context testRun

[<EntryPoint>]
let main argv =
    match parseOptions argv |> Option.map parseSuccess with
    | Some _ -> 0
    | None -> 1
