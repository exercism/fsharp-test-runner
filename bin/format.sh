#!/bin/bash

dotnet tool restore
dotnet fantomas --recurse ./src/Exercism.TestRunner.FSharp
