module ClassBasedTestsTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open ClassBasedTests

type Tests() =
    [<Test>]
    member _.``Add should add numbers``() = add 1 1 |> should equal 2

    [<Test>]
    member _.``Sub should subtract numbers``() = sub 7 3 |> should equal 4

    [<Test>]
    member _.``Mul should multiply numbers``() = mul 2 3 |> should equal 6
