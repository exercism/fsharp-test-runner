module FooTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open Foo

[<Test>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2
