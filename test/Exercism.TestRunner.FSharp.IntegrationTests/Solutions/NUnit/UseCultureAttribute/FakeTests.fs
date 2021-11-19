module FakeTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open Fake

[<Test>]
[<SetCulture>]
let ``Use invariant culture`` () = format 1000 |> should equal "¤1,000.00"

[<Test>]
[<Ignore("Remove this Attribute to run this test")>]
[<SetCulture("nl-NL")>]
let ``Use Dutch culture`` () = format 1000 |> should equal "€ 1.000,00"

[<Test>]
[<Ignore("Remove this Attribute to run this test")>]
[<SetCulture("en-US")>]
let ``Use US culture`` () = format 1000 |> should equal "$1,000.00"
