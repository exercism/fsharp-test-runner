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

[<Fact(Timeout = 20, Skip = "Remove this Attribute to run this test")>]
let ``Add should add more numbers with timeout`` (): Task =
    Task.Delay(TimeSpan.FromMilliseconds(100.0))

[<Theory(Skip = "Remove this Attribute to run this test")>]
[<InlineData(4, 7, 3)>]
let ``Sub should subtract numbers`` (expected, x, y) = sub x y |> should equal expected

[<CustomPropertyAttribute(Skip = "Remove this Attribute to run this test")>]
let ``Mul should multiply numbers`` (x, y) = mul x y |> should equal (x * y)

[<Property(Skip = "Remove this Attribute to run this test")>]
let ``Div should divide numbers`` (x) : Property =
    Prop.throws<DivideByZeroException, int> (new Lazy<int>(fun () -> x / 0))
