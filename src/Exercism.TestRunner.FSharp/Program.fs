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
    let testFile = Path.Combine(options.InputDirectory, sprintf "%sTest.fs" exercise)
    let projectFile = Path.Combine(options.InputDirectory, sprintf "%s.fsproj" exercise)
    let resultsFile = Path.Combine(options.OutputDirectory, "results.json")

    { InputFile = inputFile
      TestFile = testFile
      ProjectFile = projectFile
      ResultsFile = resultsFile }

let private parseOptions argv =
    let parserResult = CommandLine.Parser.Default.ParseArguments<Options>(argv)
    match parserResult with
    | :? (Parsed<Options>) as options -> Some options.Value
    | _ -> None

let private parseSuccess options =
    let context = createTestRunContext options
    let result = compileProject context |> testRunFromCompilationResult

    writeTestResults context result

[<EntryPoint>]
let main argv =
    match parseOptions argv with
    | Some options ->
        parseSuccess options
        0
    | None -> 1
