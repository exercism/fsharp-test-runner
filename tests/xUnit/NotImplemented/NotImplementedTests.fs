module NotImplementedTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open NotImplemented

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2
