module DifferentTypesOfTestsTests

open System
open NUnit.Framework
open FsUnit
open FsCheck
open FsCheck.NUnit
open Exercism.Tests
open DifferentTypesOfTests

type CustomPropertyAttribute() =
    inherit PropertyAttribute()

type Letters =
    static member Chars () =
        Arb.Default.Char()
        |> Arb.filter (fun c -> 'A' <= c && c <= 'Z')    

type LetterAttribute () =
    inherit PropertyAttribute(Arbitrary = [| typeof<Letters> |])

[<Test>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Test>]
let ``Add should add more numbers`` () = add 2 3 |> should equal 5

[<Test(ExpectedResult = 5)>]
let ``Add should add more numbers with expected`` () = add 2 3

[<TestCase(4, 7, 3)>]
let ``Sub should subtract numbers`` (expected, x, y) = sub x y |> should equal expected

[<CustomPropertyAttribute>]
let ``Mul should multiply numbers`` (x, y) = mul x y |> should equal (x * y)

[<LetterAttribute>]
let ``Letter should be uppercase`` (letter) = Char.IsUpper(letter) |> should equal true

[<Property>]
let ``Div should divide numbers`` (x) : Property =
    Prop.throws<DivideByZeroException, int> (new Lazy<int>(fun () -> x / 0))
