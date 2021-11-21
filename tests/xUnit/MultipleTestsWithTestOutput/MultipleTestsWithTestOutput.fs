module MultipleTestsWithTestOutput

let add x y =
    printf "Printf and"
    printfn "printfn output"

    x + y

let sub x y =
    System.Console.WriteLine("Output")
    System.Diagnostics.Trace.WriteLine("from")
    System.Diagnostics.Debug.WriteLine("multiple")
    System.Console.Out.WriteLine("sources")

    x * y + 1

let mul x y = x * y
