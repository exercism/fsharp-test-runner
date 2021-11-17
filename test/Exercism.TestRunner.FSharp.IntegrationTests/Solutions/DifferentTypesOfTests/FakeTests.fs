module FakeTests

open System
open Xunit
open FsUnit.Xunit
open FsCheck
open FsCheck.Xunit
open Exercism.Tests
open Fake

type CustomPropertyAttribute() =
    inherit PropertyAttribute()

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Fact>]
let ``Add should add more numbers`` () = add 2 3 |> should equal 5

[<Theory>]
[<InlineData(4, 7, 3)>]
let ``Sub should subtract numbers`` (expected, x, y) = sub x y |> should equal expected

[<CustomPropertyAttribute>]
let ``Mul should multiply numbers`` (x, y) = mul x y |> should equal (x * y)

[<Property>]
let ``Div should divide numbers`` (x) : Property =
    Prop.throws<DivideByZeroException, int> (new Lazy<int>(fun () -> x / 0))
