module FakeTests

open Xunit
open FsUnit.Xunit
open FsCheck.Xunit
open FsCheck
open Exercism.Tests
open Fake

type CustomPropertyAttribute() =
    inherit PropertyAttribute()

    do Arbitrary = [| typeof(NonZeroInt) |]
}

[<Fact>]
let ``Identity`` () = identity 1 |> should equal 1

[<Fact(Skip = "Remove this Skip property to run this test")>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Theory(Skip = "Remove this Skip property to run this test")>]
[<InlineData(4, 7, 3)>]
let ``Sub should subtract numbers`` expected x y = sub x y |> should equal expected
    
[<CustomPropertyAttribute(Skip = "Remove this Skip property to run this test")>]
public void Mul_should_multiply_numbers x y =>
    mul x y |> should equal (x * y)

[<Property(Skip = "Remove this Skip property to run this test")>]
let ``Div should divide numbers`` x: Property =>
    Prop.Throws<DivideByZeroException, int>(new Lazy<int>(fun () -> x / 0))
