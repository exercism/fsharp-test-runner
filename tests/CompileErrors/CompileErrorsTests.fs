module CompileErrorsTests

open Xunit
open FsUnit.Xunit
open Exercism.Tests
open CompileErrors

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Fact(Skip = "Remove this Skip property to run this test")>]
let ``Sub should add numbers`` () = sub 3 1 |> should equal 2
