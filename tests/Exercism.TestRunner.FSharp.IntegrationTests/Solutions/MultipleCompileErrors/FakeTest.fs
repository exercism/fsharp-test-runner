module FakeTest

open Xunit
open FsUnit.Xunit
open Fake

[<Fact>]
let Add_should_add_numbers() = Add 1 1 |> should equal 2

[<Fact>]
let Sub_should_add_numbers() = Sub 3 1 |> should equal 2
