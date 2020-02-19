module Exercism.TestRunner.FSharp.Testing

open System.Reflection
open Xunit
open Xunit.Abstractions
open Xunit.Sdk
open Exercism.TestRunner.FSharp.Core
open Exercism.TestRunner.FSharp.Utils
open Exercism.TestRunner.FSharp.Project
open FSharp.Compiler.SourceCodeServices

let private sourceInformationProvider = new NullSourceInformationProvider()
let private diagnosticMessageSink = new TestMessageSink()
let private executionMessageSink = new TestMessageSink()

let private findTestCases (assemblyInfo: IAssemblyInfo) =
    use discoverySink = new TestDiscoverySink()
    use discoverer = new XunitTestFrameworkDiscoverer(assemblyInfo, sourceInformationProvider, diagnosticMessageSink)

    discoverer.Find(false, discoverySink, TestFrameworkOptions.ForDiscovery())
    discoverySink.Finished.WaitOne() |> ignore

    discoverySink.TestCases
    |> Seq.cast<IXunitTestCase>
    |> Seq.toArray

let private createTestAssemblyRunner testCases testAssembly =
    new XunitTestAssemblyRunner(
        testAssembly,
        testCases,
        diagnosticMessageSink,
        executionMessageSink,
        TestFrameworkOptions.ForExecution());

let private testResultFromPass (passedTest: ITestPassed) =
    { Name = passedTest.TestCase.DisplayName
      Status = TestStatus.Pass
      Message = None
      Output = Option.ofNonEmptyString passedTest.Output }

let private failureToMessage messages =
    messages
    |> Array.map String.normalize
    |> Array.map (fun message -> message.Replace("Exception of type 'FsUnit.Xunit+MatchException' was thrown.\n", ""))
    |> String.concat "\n"

let private testResultFromFailed (failedTest: ITestFailed) =
    { Name = failedTest.TestCase.DisplayName
      Status = TestStatus.Fail
      Message = Some (failureToMessage failedTest.Messages)
      Output = Option.ofNonEmptyString failedTest.Output }
    
let private runTests (assembly: Assembly) =
    let assemblyInfo = Reflector.Wrap(assembly)
    let testAssembly = TestAssembly(assemblyInfo)

    let testResults = ResizeArray()
    executionMessageSink.Execution.add_TestFailedEvent(fun args -> testResults.Add(testResultFromFailed(args.Message)))
    executionMessageSink.Execution.add_TestPassedEvent(fun args -> testResults.Add(testResultFromPass(args.Message)))

    let testCases = findTestCases assemblyInfo

    use assemblyRunner = createTestAssemblyRunner testCases testAssembly
    assemblyRunner.RunAsync()
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> ignore
    
    Seq.toList testResults

let private testRunStatusFromTest (tests: TestResult list) =
    let statuses =
        tests
        |> List.map (fun test -> test.Status)
        |> set
    
    if Set.contains Fail statuses then
        Fail
    elif Set.singleton Pass = statuses then
        Pass
    else
        Error

let private testRunFromTests tests =
    { Message = None
      Status = testRunStatusFromTest tests
      Tests = tests }
    
let private testRunFromError error =
    { Message = Some error
      Status = Error
      Tests = [] }

let private errorToMessage (error: FSharpErrorInfo) =
    let fileName = System.IO.Path.GetFileName(error.FileName)
    let lineNumber = error.StartLineAlternate
    let message = String.normalize error.Message
    
    sprintf "%s:%i: %s" fileName lineNumber message
    
let private testRunFromCompilationError compilationError =
    match compilationError with
    | CompilationError errors ->
        testRunFromError (errors |> Array.map errorToMessage |> String.concat "\n")
    | CompilationFailed ->
        testRunFromError "Could not compile project"
    | ProjectNotFound ->
        testRunFromError "Could not find project file"
    | TestFileNotFound ->
        testRunFromError "Could not find test file"

let private testRunFromCompiledAssembly (assembly: Assembly) =
    assembly
    |> runTests
    |> testRunFromTests
    
let testRunFromCompilationResult result =
    match result with
    | Result.Ok assembly -> testRunFromCompiledAssembly assembly
    | Result.Error error -> testRunFromCompilationError error