module Exercism.TestRunner.FSharp.Utils

module String =
    let normalize (str: string) = str.Replace("\r\n", "\n")

module File =
    open System.IO
    open System.Text.RegularExpressions

    let regexReplace (file: string) (pattern: string) (replacement: string) =
        let contents = File.ReadAllText(file)
        let replacedContents = Regex.Replace(contents, pattern, replacement)
        File.WriteAllText(file, replacedContents)

module Option =
    let ofNonEmptyString (str: string) =
        if str = null || str = "" then None
        else Some str

    let toNullableString (opt: string option) = Option.defaultValue null opt

module Process =
    let exec fileName arguments workingDirectory =
        let psi = System.Diagnostics.ProcessStartInfo()
        psi.FileName <- fileName
        psi.Arguments <- arguments
        psi.WorkingDirectory <- workingDirectory
        psi.CreateNoWindow <- true
        psi.UseShellExecute <- false

        use p = new System.Diagnostics.Process()
        p.StartInfo <- psi

        p.Start() |> ignore
        p.WaitForExit()

        if p.ExitCode = 0 then Result.Ok()
        else Result.Error()
