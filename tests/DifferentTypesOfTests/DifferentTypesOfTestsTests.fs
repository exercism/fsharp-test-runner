module DifferentTypesOfTestsTests

open System
open System.Threading.Tasks

open FsCheck.FSharp
open Xunit
open FsUnit.Xunit
open FsCheck
open FsCheck.Xunit
open DifferentTypesOfTests

type CustomPropertyAttribute() =
    inherit PropertyAttribute()

type Letters =
    static member Chars () =
        ArbMap.defaults
        |> ArbMap.arbitrary<char>
        |> Arb.mapFilter id (fun c -> 'A' <= c && c <= 'Z')

type LetterAttribute () =
    inherit PropertyAttribute(Arbitrary = [| typeof<Letters> |])

[<Fact>]
let ``Add should add numbers`` () = add 1 1 |> should equal 2

[<Fact(Skip = "Remove this Skip property to run this test")>]
let ``Add should add more numbers`` () = add 2 3 |> should equal 5

[<Fact(Timeout = 20, Skip = "Remove this Skip property to run this test")>]
let ``Add should add more numbers with timeout`` (): Task =
    Task.Delay(TimeSpan.FromMilliseconds(100.0))

[<Theory(Skip = "Remove this Skip property to run this test")>]
[<InlineData(4, 7, 3)>]
let ``Sub should subtract numbers`` (expected, x, y) = sub x y |> should equal expected

[<CustomPropertyAttribute(Skip = "Remove this Skip property to run this test")>]
let ``Mul should multiply numbers`` (x, y) = mul x y |> should equal (x * y)

[<LetterAttribute(Skip = "Remove this Skip property to run this test")>]
let ``Letter should be uppercase`` (letter) = Char.IsUpper(letter) |> should equal true

[<Property(Skip = "Remove this Skip property to run this test")>]
let ``Div should divide numbers`` (x) : Property =
    Prop.throws<DivideByZeroException, int> (new Lazy<int>(fun () -> x / 0))

type ClassBasedTests() =
    [<Fact>]
    member _.``Add should add numbers``() = add 1 1 |> should equal 2

    [<Fact>]
    member _.``Sub should subtract numbers``() = sub 7 3 |> should equal 4

    [<Fact>]
    member _.``Mul should multiply numbers``() = mul 2 3 |> should equal 6
