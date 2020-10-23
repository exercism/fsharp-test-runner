module Exercism.TestRunner.FSharp.Output

open System.Text.Json
open System.IO
open System.Text.Json.Serialization
open Exercism.TestRunner.FSharp.Core

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
jsonSerializerOptions.IgnoreNullValues <- true

let private toJsonTestStatus (testStatus: TestStatus) =
    match testStatus with
    | Pass -> "pass"
    | Fail -> "fail"
    | Error -> "error"

let private toJsonTestResult (testResult: TestResult) =
    { Name = testResult.Name
      Message = testResult.Message |> Option.toObj
      Output = testResult.Output |> Option.toObj
      Status = toJsonTestStatus testResult.Status }

let private toJsonTestRun (testRun: TestRun) =
    { Message = testRun.Message |> Option.toObj
      Status = toJsonTestStatus testRun.Status
      Tests =
          testRun.Tests
          |> Seq.map toJsonTestResult
          |> Seq.toArray }

let private serializeTestResults (testRun: TestRun) =
    JsonSerializer.Serialize(toJsonTestRun testRun, jsonSerializerOptions)

let writeTestResults context testRun =
    File.WriteAllText(context.ResultsFile, serializeTestResults testRun)
