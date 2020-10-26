<#
.SYNOPSIS
    Format the F# source code.
.DESCRIPTION
    Formats the F# source code.
.EXAMPLE
    The example below will format all F# source code
    PS C:\> ./format.ps1
#>

dotnet tool restore

# TODO: make this simpler once the fantomasignore file works well with subdirectories
dotnet fantomas --recurse ./src/Exercism.TestRunner.FSharp
dotnet fantomas ./tests/Exercism.TestRunner.FSharp.IntegrationTests/Tests.fs