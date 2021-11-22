module SomeTestsWithTaskTests

open NUnit.Framework
open FsUnit
open Exercism.Tests
open SomeTestsWithTask

[<Test>]
[<Task(1)>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Test>]
[<Ignore("Remove this Attribute to run this test")>]
let ``Sub should subtract numbers`` () = sub 7 3 |> should equal 4

[<Task(3)>]
[<Test>]
[<Ignore("Remove this Attribute to run this test")>]
let ``Mul should multiply numbers`` () = mul 2 3 |> should equal 6
