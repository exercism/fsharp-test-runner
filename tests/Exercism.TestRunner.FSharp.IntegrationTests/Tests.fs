module Exercism.TestRunner.FSharp.IntegrationTests.Tests

open System.IO
open Xunit
open FsUnit.Xunit

open Exercism.TestRunner.FSharp.IntegrationTests.Helpers

type TestRun =
    { Expected: string
      Actual: string }

type TestSolution =
    { Slug: string
      Directory: string
      DirectoryName: string }

let private runTestRunner testSolution =
    let run() =
        let testScript = Directory.findFileRecursively "run.ps1"
        let testScriptArguments = [ testSolution.Slug; testSolution.Directory; testSolution.Directory ]
        let powershellArguments = testScript :: testScriptArguments
        Process.run "pwsh" powershellArguments

    let readTestRunResults() =
        let readTestRunResultFile fileName =
            Path.Combine(testSolution.Directory, fileName)
            |> File.ReadAllText
            |> Json.normalize

        { Expected = readTestRunResultFile "results.json"
          Actual = readTestRunResultFile "expected_results.json" }

    run()
    readTestRunResults()

let private assertSolutionHasExpectedResults (directory: string) =
    let testSolutionDirectory = Path.GetFullPath(Path.Combine([| "Solutions"; directory |]))

    let testSolution =
        { Slug = "Fake"
          Directory = testSolutionDirectory
          DirectoryName = Path.GetFileName(testSolutionDirectory) }

    let testRun = runTestRunner testSolution
    testRun.Actual |> should equal testRun.Expected

[<Fact>]
let ``Single compile error``() = assertSolutionHasExpectedResults "SingleCompileError"

[<Fact>]
let ``Multiple compile errors``() = assertSolutionHasExpectedResults "MultipleCompileErrors"

[<Fact>]
let ``Multiple tests that pass``() = assertSolutionHasExpectedResults "MultipleTestsWithAllPasses"

[<Fact>]
let ``Multiple tests and single fail``() = assertSolutionHasExpectedResults "MultipleTestsWithSingleFail"

[<Fact>]
let ``Multiple tests and multiple fails``() = assertSolutionHasExpectedResults "MultipleTestsWithMultipleFails"

[<Fact>]
let ``Single test that passes``() = assertSolutionHasExpectedResults "SingleTestThatPasses"

[<Fact>]
let ``Single test that fails``() = assertSolutionHasExpectedResults "SingleTestThatFails"

[<Fact>]
let ``Not implemented``() = assertSolutionHasExpectedResults "NotImplemented"

[<Fact>]
let ``NetCoreApp2.1 solution``() = assertSolutionHasExpectedResults "NetCoreApp2.1"

[<Fact>]
let ``NetCoreApp2.2 solution``() = assertSolutionHasExpectedResults "NetCoreApp2.2"

[<Fact>]
let ``NetCoreApp3.0 solution``() = assertSolutionHasExpectedResults "NetCoreApp3.0"
