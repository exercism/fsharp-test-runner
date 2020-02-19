module Exercism.TestRunner.FSharp.IntegrationTests.Helpers

module String =
    let normalize (str: string) = str.Replace("\r\n", "\n")
