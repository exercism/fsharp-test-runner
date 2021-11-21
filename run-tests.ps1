<#
.SYNOPSIS
    Run all tests.
.DESCRIPTION
    Run all tests, verifying the behavior of the test runner.
.EXAMPLE
    The example below will run all tests
    PS C:\> ./run-tests.ps1
#>

dotnet test test/Exercism.TestRunner.FSharp.IntegrationTests

exit $LastExitCode
