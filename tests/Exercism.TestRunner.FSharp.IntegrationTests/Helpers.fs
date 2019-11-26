module Exercism.TestRunner.FSharp.IntegrationTests.Helpers

module Process =
    open System.Diagnostics

    let run fileName arguments = Process.Start(fileName, String.concat " " arguments).WaitForExit()

module Directory =
    open System.IO

    let private directoryAndAncestors directory =
        directory
        |> DirectoryInfo
        |> Seq.unfold (fun currentDir ->
            if currentDir.Exists then Some(currentDir, currentDir.Parent)
            else None)

    let findFileRecursively (file: string) =
        Directory.GetCurrentDirectory()
        |> directoryAndAncestors
        |> Seq.collect (fun directory -> Directory.EnumerateFiles(directory.FullName))
        |> Seq.find (fun fileInDirectory -> Path.GetFileName(fileInDirectory) = file)

module Json =

    open System
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization

    let private serializerSettings =
        let contractResolver = DefaultContractResolver()
        contractResolver.NamingStrategy <- SnakeCaseNamingStrategy()

        let settings = JsonSerializerSettings()
        settings.ContractResolver <- contractResolver
        settings

    let private normalizeNewlines (json: string) = json.Replace("\n", Environment.NewLine)

    let private deserialize json = JsonConvert.DeserializeObject(json)

    let private serialize obj = JsonConvert.SerializeObject(obj, Formatting.None, serializerSettings)

    let normalize json =
        json
        |> deserialize
        |> serialize
        |> normalizeNewlines
