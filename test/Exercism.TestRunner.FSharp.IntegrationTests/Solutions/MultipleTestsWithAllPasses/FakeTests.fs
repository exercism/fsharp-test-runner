module FakeTests

open Xunit
open FsUnit.Xunit
open Fake

type Tests(testOutput: Xunit.Abstractions.ITestOutputHelper) =
    class
        [<Fact>]
        let ``Add should add numbers`` () = add 1 1 |> should equal 2

        [<Fact>]
        let ``Sub should subtract numbers`` () = sub 7 3 |> should equal 4

        [<Fact>]
        let ``Mul should multiply numbers`` () = mul 2 3 |> should equal 6

        let stringWriter = new System.IO.StringWriter()
        do
            System.Console.SetOut(stringWriter)
            System.Console.SetError(stringWriter)
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener())
            |> ignore

        interface System.IDisposable with
            override __.Dispose() =
                testOutput.WriteLine(stringWriter.ToString())
    end
