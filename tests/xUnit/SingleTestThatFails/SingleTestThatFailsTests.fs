module SingleTestThatFailsTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open SingleTestThatFails

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 3
