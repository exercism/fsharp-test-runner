module SingleTestThatPassesTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open SingleTestThatPasses

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2
