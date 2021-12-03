module TestWithParenthesesTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open TestWithParentheses

[<Fact>]
let ``Add should add numbers (okay-ish)`` () = add 1 1 |> should equal 2
