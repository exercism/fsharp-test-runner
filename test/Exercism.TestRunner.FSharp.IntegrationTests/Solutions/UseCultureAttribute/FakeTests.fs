module FakeTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open Fake

[<Fact>]
[<UseCulture>]
let ``Use invariant culture`` () = format 1000 |> should equal "¤1,000.00"

[<Fact>]
[<UseCulture("nl-NL")>]
let ``Use Dutch culture`` () = format 1000 |> should equal "€ 1.000,00"

[<Fact>]
[<UseCulture("en-US")>]
let ``Use US culture`` () = format 1000 |> should equal "$1,000.00"
