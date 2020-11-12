module Exercism.TestRunner.FSharp.IntegrationTests.Tests

open System.IO
open Xunit
open FsUnit.Xunit

open System.Text.Encodings.Web
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
      Output: string }

type JsonTestRun =
    { [<JsonPropertyName("status")>]
      Status: string
      [<JsonPropertyName("message")>]
      Message: string
      [<JsonPropertyName("tests")>]
      Tests: JsonTestResult [] }

let private jsonSerializerOptions = JsonSerializerOptions()

jsonSerializerOptions.Converters.Add(JsonFSharpConverter())
jsonSerializerOptions.Encoder <- JavaScriptEncoder.UnsafeRelaxedJsonEscaping
jsonSerializerOptions.IgnoreNullValues <- true

let normalizeTestRunResultJson (json: string) =
    let jsonTestRun =
        JsonSerializer.Deserialize<JsonTestRun>(json, jsonSerializerOptions)

    let normalizedJsonTestRun =
        { jsonTestRun with
              Tests =
                  jsonTestRun.Tests
                  |> Array.sortBy (fun test -> test.Name) }

    let normalizeWhitespace (str: string) = str.Replace("\r\n", "\n")

    JsonSerializer.Serialize(normalizedJsonTestRun, jsonSerializerOptions)
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
let ``Multiple tests with test ouput`` () =
    assertSolutionHasExpectedResults "MultipleTestsWithTestOutput"

[<Fact>]
let ``Multiple tests with test ouput exceeding limit`` () =
    assertSolutionHasExpectedResults "MultipleTestsWithTestOutputExceedingLimit"
