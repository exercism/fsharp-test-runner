module FakeTest

open Xunit
open FsUnit.Xunit
open Fake

[<Fact>]
let Add_should_add_numbers() = add 1 1 |> should equal 2

[<Fact>]
let Sub_should_subtract_numbers() = sub 2 1 |> should equal 1

[<Fact>]
let Mul_should_multiply_numbers() = mul 2 3 |> should equal 5
