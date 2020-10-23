module Exercism.TestRunner.FSharp.Core

type TestStatus =
    | Pass
    | Fail
    | Error

type TestResult =
    { Name: string
      Message: string option
      Output: string option
      Status: TestStatus }

type TestRun =
    { Message: string option
      Status: TestStatus
      Tests: TestResult list }

type TestRunContext =
    { InputFile: string
      TestsFile: string
      ProjectFile: string
      ResultsFile: string }

//  public string TestsFilePath => Path.Combine(InputDirectory, $"{Exercise}Tests.cs");

//         public string BuildLogFilePath => Path.Combine(InputDirectory, "msbuild.log");

//         public string TestResultsFilePath => Path.Combine(InputDirectory, "TestResults", "tests.trx");

//         public string ResultsJsonFilePath => Path.GetFullPath(Path.Combine(OutputDirectory, "results.json"));

//         private string Exercise => Slug.Dehumanize().Pascalize();

type TestRunError =
    | ProjectNotFound
    | TestsFileNotFound
    | TestsFileNotParsed
    | BuildError of string []
