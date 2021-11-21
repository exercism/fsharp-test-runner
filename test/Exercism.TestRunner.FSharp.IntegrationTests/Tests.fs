module Exercism.TestRunner.FSharp.IntegrationTests.Tests

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open Xunit
open FsUnit.Xunit

open System.Text.Json
open System.Text.Json.Serialization

type TestRun = { Expected: string; Actual: string }

type TestSolution =
    { Slug: string
      Directory: string
      DirectoryName: string }

type JsonTestResult =
    { [<JsonPropertyName("name")>]
      Name: string
      [<JsonPropertyName("status")>]
      Status: string
      [<JsonPropertyName("message")>]
      Message: string
      [<JsonPropertyName("output")>]
      Output: string
      [<JsonPropertyName("test_code")>]
      TestCode: string
      [<JsonPropertyName("task_id")>]
      TaskId: Nullable<int> }

type JsonTestRun =
    { [<JsonPropertyName("version")>]
      Version: int
      [<JsonPropertyName("status")>]
      Status: string
      [<JsonPropertyName("message")>]
      Message: string
      [<JsonPropertyName("tests")>]
      Tests: JsonTestResult [] }

let private jsonSerializerOptions = JsonSerializerOptions()
jsonSerializerOptions.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull

let normalizeTestRunResultJson (json: string) =
    let jsonTestRun = JsonSerializer.Deserialize<JsonTestRun>(json, jsonSerializerOptions)
    let normalizeWhitespace (str: string) = str.Replace("\r\n", "\n")

    JsonSerializer.Serialize(jsonTestRun, jsonSerializerOptions)
    |> normalizeWhitespace

let private runTestRunner testSolution =
    let run () =
        Exercism.TestRunner.FSharp.Program.main [| testSolution.Slug
                                                   testSolution.Directory
                                                   testSolution.Directory |]

    let readTestRunResults () =
        let readTestRunResultFile fileName =
            Path.Combine(testSolution.Directory, fileName)
            |> File.ReadAllText
            |> normalizeTestRunResultJson

        { Expected = readTestRunResultFile "results.json"
          Actual = readTestRunResultFile "expected_results.json" }

    run () |> ignore
    readTestRunResults ()

let private assertSolutionHasExpectedResultsWithSlug (directory: string) (slug: string) =
    let testSolutionDirectory =
        Path.GetFullPath(Path.Combine([| "Solutions"; directory |]))

    let testSolution =
        { Slug = slug
          Directory = testSolutionDirectory
          DirectoryName = Path.GetFileName(testSolutionDirectory) }

    let testRun = runTestRunner testSolution
    testRun.Actual |> should equal testRun.Expected

let private assertSolutionHasExpectedResults (directory: string) =
    assertSolutionHasExpectedResultsWithSlug directory "Fake"

[<Fact>]
let ``Single compile error`` () =
    assertSolutionHasExpectedResults "SingleCompileError"

[<Fact>]
let ``Multiple compile errors`` () =
    assertSolutionHasExpectedResults "MultipleCompileErrors"

[<Fact>]
let ``Multiple tests that pass`` () =
    assertSolutionHasExpectedResults "MultipleTestsWithAllPasses"

[<Fact>]
let ``Multiple tests and single fail`` () =
    assertSolutionHasExpectedResults "MultipleTestsWithSingleFail"

[<Fact>]
let ``Multiple tests and multiple fails`` () =
    assertSolutionHasExpectedResults "MultipleTestsWithMultipleFails"

[<Fact>]
let ``Single test that passes`` () =
    assertSolutionHasExpectedResults "SingleTestThatPasses"

[<Fact>]
let ``Single test that passes with different slug`` () =
    assertSolutionHasExpectedResultsWithSlug "SingleTestThatPassesWithDifferentSlug" "Foo"

[<Fact>]
let ``Single test that fails`` () =
    assertSolutionHasExpectedResults "SingleTestThatFails"

[<Fact>]
let ``Not implemented`` () =
    assertSolutionHasExpectedResults "NotImplemented"

[<Fact>]
let ``Quoted and non-quoted tests`` () =
    assertSolutionHasExpectedResults "QuotedAndNonQuotedTests"

[<Fact>]
let ``Different test code formats`` () =
    assertSolutionHasExpectedResults "DifferentTestCodeFormats"

[<Fact>]
let ``All tests with task`` () =
    assertSolutionHasExpectedResults "AllTestsWithTask"

[<Fact>]
let ``Some tests with task`` () =
    assertSolutionHasExpectedResults "SomeTestsWithTask"

[<Fact>]
let ``UseCulture attribute`` () =
    assertSolutionHasExpectedResults "UseCultureAttribute"

[<Fact>]
let ``Different types of tests`` () =
    assertSolutionHasExpectedResults "DifferentTypesOfTests"

[<Fact>]
let ``.NET 5 project`` () =
    assertSolutionHasExpectedResults "DotnetFiveProject"
    
[<Fact>]
let ``Class-based tests`` () =
    assertSolutionHasExpectedResults "ClassBasedTests"
    