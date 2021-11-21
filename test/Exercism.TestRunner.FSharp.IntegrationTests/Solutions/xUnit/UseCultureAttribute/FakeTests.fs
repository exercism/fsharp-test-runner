module FakeTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open Fake

[<Fact>]
[<UseCulture>]
let ``Use invariant culture`` () = format 1000 |> should equal "¤1,000.00"

[<Fact(Skip = "Remove this Skip property to run this test")>]
[<UseCulture("nl-NL")>]
let ``Use Dutch culture`` () = format 1000 |> should equal "€ 1.000,00"

[<Fact(Skip = "Remove this Skip property to run this test")>]
[<UseCulture("en-US")>]
let ``Use US culture`` () = format 1000 |> should equal "$1,000.00"
