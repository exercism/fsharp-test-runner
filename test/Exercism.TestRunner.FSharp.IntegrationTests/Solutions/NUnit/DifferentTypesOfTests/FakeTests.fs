module FakeTests

open System
open System.Threading.Tasks
open NUnit.Framework
open FsUnit
open FsCheck
open FsCheck.Xunit
open Exercism.Tests
open Fake

type CustomPropertyAttribute() =
    inherit PropertyAttribute()

[<Test>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Test>]
[<Ignore("Remove this Attribute to run this test")>]
let ``Add should add more numbers`` () = add 2 3 |> should equal 5

[<Test(ExpectedResult = 5)>]
[<Ignore("Remove this Attribute to run this test")>]
let ``Add should add more numbers with expected`` () =
    add 2 3

[<TestCase(4)>]
[<TestCase(7)>]
[<TestCase(3)>]
[<Ignore("Remove this Attribute to run this test")>]
let ``Sub should subtract numbers`` (expected, x, y) = sub x y |> should equal expected

[<CustomPropertyAttribute(Skip = "Remove this Attribute to run this test")>]
let ``Mul should multiply numbers`` (x, y) = mul x y |> should equal (x * y)

[<Property(Skip = "Remove this Attribute to run this test")>]
let ``Div should divide numbers`` (x) : Property =
    Prop.throws<DivideByZeroException, int> (new Lazy<int>(fun () -> x / 0))
