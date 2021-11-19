module Exercism.TestRunner.FSharp.Output

open System
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
jsonSerializerOptions.WriteIndented <- true

let private toJsonTestStatus (testStatus: TestStatus) =
    match testStatus with
    | Pass -> "pass"
    | Fail -> "fail"
    | Error -> "error"

let private toJsonTestResult (testResult: TestResult) =
    { Name = testResult.Name
      Message = testResult.Message |> Option.toObj
      TestCode = testResult.TestCode
      TaskId = testResult.TaskId |> Option.toNullable
      Status = toJsonTestStatus testResult.Status }

let private toJsonTestRun (testRun: TestRun) =
    { Version = 3
      Message = testRun.Message |> Option.toObj
      Status = toJsonTestStatus testRun.Status
      Tests =
          testRun.Tests
          |> Seq.map toJsonTestResult
          |> Seq.toArray }

let private serializeTestResults (testRun: TestRun) =
    JsonSerializer.Serialize(toJsonTestRun testRun, jsonSerializerOptions)

let writeTestResults context testRun =
    File.WriteAllText(context.ResultsFile, serializeTestResults testRun)
