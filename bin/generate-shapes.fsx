#!/usr/bin/env -S dotnet fsi

// Writes the numbered folders in shapes/ from the history of the exercism/fsharp track repo.
//
// A shape is a distinct xUnit v3 project file, ignoring the project name, its Compile items and the
// settings that don't affect compiling. This script reads every exercise's .fsproj in every commit since
// the track switched to xUnit v3, groups them by shape, and writes one template project file per shape to
// <out_dir>/<n>/ExercismShape.fsproj, with net9.0 replaced by net10.0 as the runner does. The Dockerfile
// builds each one (bin/build-shapes.sh), so that the fast path can reuse its compiler arguments.
//
// The grouping here is close to FastPath.shapeKey in the runner. If the two ever disagree, the worst case
// will be a duplicate template, or a project file with no matching template, which uses MSBuild.
//
// Usage, from the repo root:
//   dotnet fsi bin/generate-shapes.fsx shapes [path to the exercism/fsharp repo, default ../fsharp]

#r "nuget: LibGit2Sharp, 0.32.0"

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml.Linq
open LibGit2Sharp

/// A shape's key: a string, or a list of keys. Keys sort and print like Python tuples of strings, which
/// keeps the order of the shapes the same as when the shapes were first generated.
type Node =
    | Text of string
    | Children of Node list

let rec compareNodes left right =
    match left, right with
    | Text a, Text b -> String.CompareOrdinal(a, b)
    | Text _, Children _ -> 1
    | Children _, Text _ -> -1
    | Children a, Children b ->
        Seq.zip a b
        |> Seq.map (fun (x, y) -> compareNodes x y)
        |> Seq.tryFind (fun comparison -> comparison <> 0)
        |> Option.defaultValue (compare a.Length b.Length)

/// Quotes a string as Python's repr() does.
let quote (value: string) =
    let quote = if value.Contains('\'') && not (value.Contains('"')) then '"' else '\''
    let builder = StringBuilder().Append(quote)

    for c in value do
        builder.Append(
            match c with
            | '\\' -> @"\\"
            | '\n' -> @"\n"
            | '\r' -> @"\r"
            | '\t' -> @"\t"
            | _ when c = quote -> $"\\{c}"
            | _ when c < ' ' || c = '\u007f' -> $"\\x{int c:x2}"
            | _ -> string c
        )
        |> ignore

    builder.Append(quote).ToString()

let rec nodeText node =
    match node with
    | Text text -> quote text
    | Children [ child ] -> $"({nodeText child},)"
    | Children children -> $"""({children |> List.map nodeText |> String.concat ", "})"""

let sortNodes nodes = List.sortWith compareNodes nodes

let ignoredProperties = set [ "IsPackable"; "RootNamespace" ]

/// The element's text before its first child element, trimmed.
let leadingText (element: XElement) =
    element.Nodes()
    |> Seq.takeWhile (fun node -> not (node :? XElement))
    |> Seq.choose (fun node ->
        match node with
        | :? XText as text -> Some text.Value
        | _ -> None)
    |> String.concat ""
    |> fun text -> text.Trim()

/// Reduces a project file to its properties and items (apart from Compile items), sorted, ignoring the
/// properties that don't affect compiling.
let key (text: string) =
    let root = XElement.Parse(text.TrimStart('﻿'))
    let properties = SortedDictionary<string, string>(StringComparer.Ordinal)
    let items = List<Node>()

    for group in root.Elements() do
        for element in group.Elements() do
            if group.Name = XName.Get "PropertyGroup" then
                if not (ignoredProperties.Contains(element.Name.ToString())) then
                    properties[element.Name.ToString()] <- leadingText element
            elif element.Name <> XName.Get "Compile" then
                let attributes =
                    element.Attributes()
                    |> Seq.map (fun attribute -> Children [ Text(attribute.Name.ToString()); Text attribute.Value ])
                    |> List.ofSeq
                    |> sortNodes

                let metadata =
                    element.Elements()
                    |> Seq.map (fun child -> Children [ Text(child.Name.ToString()); Text(leadingText child) ])
                    |> List.ofSeq
                    |> sortNodes

                items.Add(Children [ Text(element.Name.ToString()); Children(attributes @ metadata) ])

    Children
        [ Children [ for property in properties -> Children [ Text property.Key; Text property.Value ] ]
          Children(items |> List.ofSeq |> sortNodes) ]

let compileItems = Regex("""(\s*<Compile Include="[^"]*" />)+""")

/// Keeps the project file as it is, with the Compile items replaced by the template's two files and no RootNamespace.
/// The two files are shared by every template, so they sit in the shapes/ folder itself.
let template (text: string) =
    let text = text.TrimStart('﻿').Replace("net9.0", "net10.0")

    let text =
        compileItems.Replace(
            text,
            "\n    <Compile Include=\"../ExercismShape.fs\" />\n    <Compile Include=\"../ExercismShapeTests.fs\" />",
            1
        )

    Regex.Replace(text, @"\s*<RootNamespace>[^<]*</RootNamespace>", "")

/// Every .fsproj file in the tree and its subfolders, in the order `git ls-tree -r` lists them.
let rec projectFiles (tree: Tree) (path: string) : seq<string * Blob> =
    seq {
        for entry in tree do
            let entryPath = $"{path}/{entry.Name}"

            match entry.Target with
            | :? Tree as subtree -> yield! projectFiles subtree entryPath
            | :? Blob as blob when entry.Name.EndsWith(".fsproj", StringComparison.Ordinal) -> yield entryPath, blob
            | _ -> ()
    }

/// Returns the file as text, with Windows and old Mac line endings turned into \n.
let blobText (blob: Blob) =
    use stream = blob.GetContentStream()
    use memory = new MemoryStream()
    stream.CopyTo(memory)
    let text = UTF8Encoding(false, true).GetString(memory.ToArray())
    text.Replace("\r\n", "\n").Replace('\r', '\n')

type Shape = { Key: Node; Text: string; Exercises: HashSet<string> }

let generate (outDirectory: string) (trackDirectory: string) =
    // Each shape keeps the first project file found for it, and the exercises that use it.
    let shapes = Dictionary<string, Shape>()

    do
        use repository = new Repository(trackDirectory)

        // Read the commits newest first. Commits that leave the exercises folder as it was would find the
        // same files again, so they are skipped.
        let commits =
            repository.Commits.QueryBy(CommitFilter(SortBy = (CommitSortStrategies.Topological ||| CommitSortStrategies.Time)))
            |> Seq.filter (fun commit -> not (isNull commit["exercises"]))
            |> Seq.distinctBy (fun commit -> commit["exercises"].Target.Id)

        use enumerator = commits.GetEnumerator()
        let mutable finished = false

        while not finished && enumerator.MoveNext() do
            let commit = enumerator.Current
            let mutable found = false

            for path, blob in projectFiles (commit["exercises"].Target :?> Tree) "exercises" do
                let text = (blobText blob).Replace("net9.0", "net10.0")

                if text.Contains("xunit.v3", StringComparison.Ordinal) then
                    found <- true
                    let key = key text
                    let keyText = nodeText key

                    if not (shapes.ContainsKey keyText) then
                        shapes[keyText] <- { Key = key; Text = text; Exercises = HashSet() }

                    shapes[keyText].Exercises.Add(path.Split('/')[2]) |> ignore

            // Commits from before the track switched to xUnit v3 have no xUnit v3 project files, so stop at the first one.
            if not found then finished <- true

    // Replace only the numbered folders, so that the shared files in the output folder stay.
    if Directory.Exists outDirectory then
        for directory in Directory.EnumerateDirectories outDirectory |> Seq.toArray do
            if Path.GetFileName(directory) |> Seq.forall Char.IsAsciiDigit then
                Directory.Delete(directory, true)

    // The most used shapes come first. Shapes used by the same number of exercises are ordered by their key.
    let ordered =
        shapes.Values
        |> Seq.sortWith (fun a b ->
            match compare b.Exercises.Count a.Exercises.Count with
            | 0 -> String.CompareOrdinal(nodeText a.Key, nodeText b.Key)
            | comparison -> comparison)
        |> Seq.toArray

    for i, shape in Array.indexed ordered do
        let directory = Path.Combine(outDirectory, i.ToString("00"))
        Directory.CreateDirectory(directory) |> ignore
        File.WriteAllText(Path.Combine(directory, "ExercismShape.fsproj"), template shape.Text, UTF8Encoding(false))
        let examples = shape.Exercises |> Seq.sortWith (fun a b -> String.CompareOrdinal(a, b)) |> Seq.truncate 3
        printfn $"""{i} {shape.Exercises.Count} {String.Join(", ", examples)}"""

match fsi.CommandLineArgs |> Array.skip 1 with
| [| outDirectory |] -> generate outDirectory "../fsharp"
| [| outDirectory; trackDirectory |] -> generate outDirectory trackDirectory
| _ ->
    eprintfn "usage: dotnet fsi bin/generate-shapes.fsx <out_dir> [path to the exercism/fsharp repo]"
    exit 1
