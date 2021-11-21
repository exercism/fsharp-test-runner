module Fake

let add x y =
    printfn "%s" (System.String('a', 498) + "bcd")

    x + y

let sub x y = x - y

let mul x y = x * y
