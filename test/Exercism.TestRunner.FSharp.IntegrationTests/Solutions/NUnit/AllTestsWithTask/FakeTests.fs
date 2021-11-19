module FakeTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open Fake

[<Test>]
[<Task(1)>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Test>]
[<Ignore("Remove this Skip property to run this test")>]
[<Task(2)>]
let ``Sub should subtract numbers`` () = sub 7 3 |> should equal 4

[<Test>]
[<Ignore("Remove this Skip property to run this test")>]
[<Task(3)>]
let ``Mul should multiply numbers`` () = mul 2 3 |> should equal 6
