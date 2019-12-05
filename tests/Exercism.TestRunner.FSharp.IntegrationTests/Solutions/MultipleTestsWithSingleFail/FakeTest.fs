module FakeTest

open Xunit
open FsUnit.Xunit
open Fake

[<Fact>]
let ``Add should add numbers``() = add 1 1 |> should equal 2

[<Fact>]
let ``Sub should subtract numbers``() = sub 2 1 |> should equal 1

[<Fact>]
let ``Mul should multiply numbers``() = mul 2 3 |> should equal 5
