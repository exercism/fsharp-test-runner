module FakeTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open Fake

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Fact(Skip = "Remove this Skip property to run this test")>]
let ``Sub should subtract numbers`` () = sub 7 3 |> should equal 4

[<Fact(Skip = "Remove this Skip property to run this test")>]
let ``Mul should multiply numbers`` () = mul 2 3 |> should equal 6
