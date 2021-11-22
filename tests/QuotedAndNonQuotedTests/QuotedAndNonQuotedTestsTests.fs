module QuotedAndNonQuotedTestsTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open QuotedAndNonQuotedTests

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Fact(Skip = "Remove this Skip property to run this test")>]
let Sub_should_subtract_numbers () = sub 3 1 |> should equal 2
