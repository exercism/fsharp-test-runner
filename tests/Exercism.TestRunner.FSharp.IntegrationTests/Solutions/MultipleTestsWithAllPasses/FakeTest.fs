module FakeTest

open Xunit
open FsUnit.Xunit
open Fake

[<Fact>]
let Add_should_add_numbers() = add 1 1 |> should equal 2

[<Fact(Skip = "Remove to run test")>]
let Sub_should_subtract_numbers() = sub 7 3 |> should equal 4

[<Fact(Skip = "Remove to run test")>]
let Mul_should_multiply_numbers() = mul 2 3 |> should equal 6
