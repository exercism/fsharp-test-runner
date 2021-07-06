module FakeTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open Fake

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Fact>]
let Sub_should_subtract_numbers () = sub 3 1 |> should equal 2
