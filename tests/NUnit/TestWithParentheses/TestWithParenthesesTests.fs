module TestWithParenthesesTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open TestWithParentheses

[<Test>]
let ``Add should add numbers (okay-ish)`` () = add 1 1 |> should equal 2
