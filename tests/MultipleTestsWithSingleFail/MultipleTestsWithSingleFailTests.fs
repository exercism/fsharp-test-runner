module MultipleTestsWithSingleFailTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open MultipleTestsWithSingleFail

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Fact(Skip = "Remove this Skip property to run this test")>]
let ``Sub should subtract numbers`` () = sub 2 1 |> should equal 1

[<Fact(Skip = "Remove this Skip property to run this test")>]
let ``Mul should multiply numbers`` () = mul 2 3 |> should equal 5
