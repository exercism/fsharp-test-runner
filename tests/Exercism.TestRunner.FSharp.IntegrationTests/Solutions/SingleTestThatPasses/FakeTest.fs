module FakeTest

open Xunit
open FsUnit.Xunit
open Fake

[<Fact>]
let ``Add_should_add_numbers`` () =
    Add 1 1 |> should equal 2
