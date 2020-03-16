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

let private createTestRunContext options =
    let exercise = options.Slug.Dehumanize().Pascalize()

    let inputFile = Path.Combine(options.InputDirectory, sprintf "%s.fs" exercise)
    let testsFile = Path.Combine(options.InputDirectory, sprintf "%sTests.fs" exercise)
    let projectFile = Path.Combine(options.InputDirectory, sprintf "%s.fsproj" exercise)
    let resultsFile = Path.Combine(options.OutputDirectory, "results.json")

    { InputFile = inputFile
      TestsFile = testsFile
      ProjectFile = projectFile
      ResultsFile = resultsFile }

let private parseSuccess options =
    let context = createTestRunContext options
    let result = compileProject context |> testRunFromCompilationResult

    writeTestResults context result

let private parseOptions argv =
    let parserResult = CommandLine.Parser.Default.ParseArguments<Options>(argv)
    match parserResult with
    | :? (Parsed<Options>) as options -> Some options.Value
    | _ -> None

[<EntryPoint>]
let main argv =
    match parseOptions argv |> Option.map parseSuccess with
    | Some _ -> 0
    | None -> 1
