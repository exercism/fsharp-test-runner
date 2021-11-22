module SingleCompileErrorTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open SingleCompileError

[<Test>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2
