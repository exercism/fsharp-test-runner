module FakeTest

open Xunit
open FsUnit.Xunit
open Fake

[<Fact>]
let ``Add_should_add_numbers`` () =
    Add 1 1 |> should equal 2

[<Fact(Skip = "Remove to run test")>]
let ``Sub_should_subtract_numbers`` () =
    Sub 7 3 |> should equal 4

[<Fact(Skip = "Remove to run test")>]
let ``Mul_should_multiply_numbers`` () =
    Mul 2 3 |> should equal 6

