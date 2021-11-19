module FakeTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open Fake

[<Test>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Test>]
[<Ignore("Remove this Skip property to run this test")>]
let Sub_should_subtract_numbers () = sub 3 1 |> should equal 2
