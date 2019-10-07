module FakeTest

open Xunit
open FsUnit.Xunit
open Fake

[<Fact>]
let ``Add_should_add_numbers`` () =
    Add 1 1 |> should equal 2

[<Fact()>]
let ``Sub_should_subtract_numbers`` () =
    Sub 2 1 |> should equal 1

[<Fact()>]
let ``Mul_should_multiply_numbers`` () =
    Mul 2 3 |> should equal 5
