module FakeTest

open Xunit
open FsUnit.Xunit
open Fake

[<Fact>]
let Add_should_add_numbers() = add 1 1 |> should equal 2

[<Fact>]
let Sub_should_add_numbers() = sub 3 1 |> should equal 2
