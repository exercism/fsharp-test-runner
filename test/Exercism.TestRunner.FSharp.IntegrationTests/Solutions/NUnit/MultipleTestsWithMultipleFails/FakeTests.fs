module FakeTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open Fake

[<Test>]
let ``Add should add numbers`` () = add 1 1 |> should equal 3

[<Test>]
[<Ignore("Remove this Skip property to run this test")>]
let ``Sub should subtract numbers`` () = sub 2 1 |> should equal 1

[<Test>]
[<Ignore("Remove this Skip property to run this test")>]
let ``Mul should multiply numbers`` () = mul 2 3 |> should equal 7
