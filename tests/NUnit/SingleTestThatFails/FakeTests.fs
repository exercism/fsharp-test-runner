module FakeTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open Fake

[<Test>]
let ``Add should add numbers`` () = add 1 1 |> should equal 3
