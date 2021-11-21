module FakeTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open Fake

type Tests() =
    [<Fact>]
    member _.``Add should add numbers``() = add 1 1 |> should equal 2

    [<Fact>]
    member _.``Sub should subtract numbers``() = sub 7 3 |> should equal 4

    [<Fact>]
    member _.``Mul should multiply numbers``() = mul 2 3 |> should equal 6
