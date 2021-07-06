module FakeTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open Fake

[<Fact>]
[<Task(1)>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Fact>]
[<Task(2)>]
let ``Sub should subtract numbers`` () = sub 7 3 |> should equal 4

[<Fact>]
[<Task(3)>]
let ``Mul should multiply numbers`` () = mul 2 3 |> should equal 6
