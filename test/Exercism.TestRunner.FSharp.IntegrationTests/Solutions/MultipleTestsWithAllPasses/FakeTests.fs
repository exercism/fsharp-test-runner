module FakeTests

open Xunit
open FsUnit.Xunit
open Fake

let ``Add should add numbers`` () = add 1 1 |> should equal 2
[<Fact>]
let ``Sub should subtract numbers`` () = sub 7 3 |> should equal 4

[<Fact>]
let ``Mul should multiply numbers`` () = mul 2 3 |> should equal 6
