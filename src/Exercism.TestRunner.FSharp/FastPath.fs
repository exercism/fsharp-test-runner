/// Compiles the solution by calling the F# compiler directly, without `dotnet restore` or MSBuild.
///
/// Why it is faster: `dotnet restore` and `dotnet build` each start MSBuild, evaluate the project and
/// resolve its packages. That costs about 3 s per run and gives the same answer for every solution with
/// the same project file. Here we only run the compiler on the solution's files.
///
/// How the project file is still respected: the shapes/ folder holds one template project for each
/// distinct project file in the track's history (a "shape"). At image build, the SDK builds each template
/// (bin/build-shapes.sh) and keeps its exact compiler command line (fsc.rsp), its generated source files
/// and its output folder with every dependency. At run time, the solution's project file is reduced to the
/// settings that affect compiling (`shapeKey`), and we only go ahead if a template has exactly the same key.
///
/// The fast path returns `NotUsed` whenever it meets something it doesn't fully understand, and the
/// caller then uses MSBuild.
module Exercism.TestRunner.FSharp.FastPath

open System
open System.IO
open System.Diagnostics
open System.Text.RegularExpressions
open System.Xml.Linq
open Exercism.TestRunner.FSharp.Core

type CompileResult =
    | Compiled of assembly: string
    | CompileErrors of errors: string[]
    | NotUsed of reason: string

let ShapesDirectory = "/opt/test-runner/shapes"

// The template project's name. We replace it with the solution's project name wherever it appears.
let private Token = "ExercismShape"

// The template's root namespace (set by bin/build-shapes.sh). The SDK and the xUnit packages put their
// generated code in the root namespace, so we replace it with the solution's.
let private RootNamespaceToken = "ExercismRootNamespace"

let private validNamespace = Regex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.Compiled)

// MSBuild uses the project name when the project file sets no RootNamespace.
let private rootNamespace (projectFileText: string) projectName =
    XElement.Parse(projectFileText).Elements(XName.Get "PropertyGroup").Elements(XName.Get "RootNamespace")
    |> Seq.tryLast
    |> Option.map (fun element -> element.Value.Trim())
    |> Option.defaultValue projectName

// Properties that don't change how the project compiles or runs.
let private ignoredProperties = set [ "IsPackable"; "RootNamespace" ]

// Metadata that a PackageReference may give as child elements. It becomes part of the key.
let private allowedPackageMetadata = set [ "IncludeAssets"; "ExcludeAssets"; "PrivateAssets" ]

// Files that could change how MSBuild builds the project. If the solution folder has any of them,
// the fast path won't run.
let private disallowedFileNames =
    set [ "directory.build.props"; "directory.build.targets"; "directory.packages.props"
          "directory.solution.props"; "global.json"; "nuget.config" ]

let private disallowedExtensions =
    set [ ".props"; ".targets"; ".sln"; ".slnx"; ".resx"; ".csproj"; ".vbproj"; ".proj" ]

/// Reduces a project file to a key that says how it compiles (its properties and package references),
/// plus its Compile items in order. Returns None for any project file with something the fast path
/// doesn't know about, such as conditions, imports, targets or other item types.
let shapeKey (projectFileText: string) : (string * string list) option =
    try
        let root = XElement.Parse(projectFileText)

        let isPlain (element: XElement) = not element.HasAttributes && not element.HasElements

        let simpleItem (element: XElement) names =
            element.Elements() |> Seq.forall (fun metadata -> allowedPackageMetadata.Contains metadata.Name.LocalName && isPlain metadata)
            && element.Attributes() |> Seq.forall (fun attribute -> List.contains attribute.Name.LocalName names)
            && element.Attribute(XName.Get "Include") <> null

        if root.Name.LocalName <> "Project"
           || Seq.length (root.Attributes()) <> 1
           || root.Attribute(XName.Get "Sdk") = null
           || root.Attribute(XName.Get "Sdk").Value <> "Microsoft.NET.Sdk" then
            None
        else
            let mutable valid = true
            let properties = Collections.Generic.SortedDictionary<string, string>(StringComparer.Ordinal)
            let packages = Collections.Generic.List<string>()
            let compileItems = Collections.Generic.List<string>()

            for group in root.Elements() do
                if group.HasAttributes then valid <- false

                match group.Name.LocalName with
                | "PropertyGroup" ->
                    for element in group.Elements() do
                        if not (isPlain element) then valid <- false
                        elif not (ignoredProperties.Contains element.Name.LocalName) then
                            properties[element.Name.LocalName] <- element.Value.Trim()
                | "ItemGroup" ->
                    for element in group.Elements() do
                        match element.Name.LocalName with
                        | "PackageReference" when simpleItem element [ "Include"; "Version" ] ->
                            let version = element.Attribute(XName.Get "Version")
                            let version = if version = null then "" else version.Value
                            let metadata =
                                element.Elements()
                                |> Seq.map (fun m -> $";{m.Name.LocalName}={m.Value.Trim()}")
                                |> Seq.sort
                                |> String.concat ""
                            let id = element.Attribute(XName.Get "Include").Value
                            packages.Add($"{id}={version}{metadata}")
                        | "Compile" when simpleItem element [ "Include" ] && not element.HasElements ->
                            compileItems.Add(element.Attribute(XName.Get "Include").Value)
                        | _ -> valid <- false
                | _ -> valid <- false

            packages.Sort(StringComparer.Ordinal)

            if not valid || compileItems.Count = 0 then
                None
            else
                let key =
                    Seq.append
                        (properties |> Seq.map (fun property -> $"{property.Key}={property.Value}"))
                        packages
                    |> String.concat "\n"

                Some(key, List.ofSeq compileItems)
    with :? Xml.XmlException ->
        None

let private findShapeDirectory key =
    if not (Directory.Exists ShapesDirectory) then
        None
    else
        Directory.EnumerateDirectories(ShapesDirectory)
        |> Seq.sort
        |> Seq.tryFind (fun directory ->
            let projectFile = Path.Combine(directory, $"{Token}.fsproj")
            File.Exists(Path.Combine(directory, "fsc.rsp"))
            && File.Exists projectFile
            && (shapeKey (File.ReadAllText projectFile) |> Option.map fst) = Some key)

// Each Compile item must be a plain .fs or .fsi file inside the solution folder.
let private sourceFiles (solutionDirectory: string) (compileItems: string list) =
    let files =
        compileItems
        |> List.map (fun item -> Path.GetFullPath(Path.Combine(solutionDirectory, item.Replace('\\', '/'))))

    let valid =
        files
        |> List.forall (fun file ->
            (file.EndsWith(".fs") || file.EndsWith(".fsi"))
            && file.StartsWith(solutionDirectory.TrimEnd('/') + "/")
            && not (file.Contains('*'))
            && File.Exists file)

    if valid then Some files else None

let private hasDisallowedFiles (solutionDirectory: string) =
    Directory.Exists(Path.Combine(solutionDirectory, "bin"))
    || Directory.Exists(Path.Combine(solutionDirectory, "obj"))
    || Directory.EnumerateFiles(solutionDirectory)
       |> Seq.exists (fun file ->
           let name = Path.GetFileName(file).ToLowerInvariant()
           disallowedFileNames.Contains name || disallowedExtensions.Contains(Path.GetExtension name))

// Builds a folder in /tmp that looks like the project's output folder: the shape's dependencies (as
// symbolic links), and its deps.json and runtimeconfig.json renamed for this project. Linking avoids
// copying the dependencies on every run. Each production run gets a fresh, empty /tmp.
let private prepareRunDirectory shapeDirectory projectName =
    let runDirectory = Path.Combine(Path.GetTempPath(), $"exercism-{Guid.NewGuid():N}")
    Directory.CreateDirectory(runDirectory) |> ignore

    for entry in Directory.EnumerateFileSystemEntries(Path.Combine(shapeDirectory, "out")) do
        let name = Path.GetFileName(entry)

        if name.StartsWith(Token + ".") then
            if name.EndsWith(".deps.json") || name.EndsWith(".runtimeconfig.json") then
                File.WriteAllText(
                    Path.Combine(runDirectory, name.Replace(Token, projectName)),
                    File.ReadAllText(entry).Replace(Token, projectName)
                )
        else
            File.CreateSymbolicLink(Path.Combine(runDirectory, name), entry) |> ignore

    runDirectory

// Turns the template's compiler arguments into the arguments for this solution: the solution's own
// files replace the template's two source files, the generated files are copied with the project name
// filled in, and the output goes to the run folder.
let private compilerArguments shapeDirectory runDirectory projectName rootNamespace (files: string list) =
    let templateFiles = set [ $"{Token}.fs"; $"{Token}Tests.fs" ]
    let mutable solutionFilesAdded = false

    File.ReadAllLines(Path.Combine(shapeDirectory, "fsc.rsp"))
    |> Array.toList
    |> List.collect (fun line ->
        if line.StartsWith("-o:") then
            [ "-o:" + Path.Combine(runDirectory, projectName + ".dll") ]
        elif line.StartsWith("--refout:") then
            []
        elif line.StartsWith("-") then
            [ line ]
        elif templateFiles.Contains line then
            if solutionFilesAdded then
                []
            else
                solutionFilesAdded <- true
                files
        else
            let path = Path.GetFullPath(Path.Combine(shapeDirectory, line))

            if path.StartsWith(shapeDirectory + "/") then
                let generated = Path.Combine(runDirectory, "generated", Path.GetFileName(path).Replace(Token, projectName))
                Directory.CreateDirectory(Path.GetDirectoryName(generated)) |> ignore
                let text = File.ReadAllText(path).Replace(RootNamespaceToken, rootNamespace).Replace(Token, projectName)
                File.WriteAllText(generated, text)
                [ generated ]
            else
                [ path ])

// fsc writes each error on one line (--flaterrors), with the full path (--fullpaths), as
// "file(line,column): error FS0001: message". MSBuild writes the same line with the project file in
// brackets after it, so we add that.
let private errorLine = Regex(@"^/.+\(\d+,\d+\): error FS\d+: ", RegexOptions.Compiled)

let private runCompiler shapeDirectory solutionDirectory responseFile =
    let fsc = File.ReadAllText(Path.Combine(shapeDirectory, "fsc.path")).Trim()
    let psi = ProcessStartInfo("dotnet", $"\"{fsc}\" @\"{responseFile}\"")
    psi.WorkingDirectory <- solutionDirectory
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    use p = Process.Start(psi)
    let stdout = p.StandardOutput.ReadToEndAsync()
    let stderr = p.StandardError.ReadToEndAsync()
    p.WaitForExit()
    p.ExitCode, stdout.Result + "\n" + stderr.Result

let compile (context: TestRunContext) : CompileResult =
    let solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(context.ProjectFile))
    let projectName = Path.GetFileNameWithoutExtension(context.ProjectFile)

    let projectFileText = File.ReadAllText context.ProjectFile

    match shapeKey projectFileText with
    | None -> NotUsed "unknown project file"
    | Some _ when not (validNamespace.IsMatch(rootNamespace projectFileText projectName)) -> NotUsed "unusual root namespace"
    | Some (key, compileItems) ->
        match findShapeDirectory key with
        | None -> NotUsed "unknown project shape"
        | Some shapeDirectory ->
            match sourceFiles solutionDirectory compileItems with
            | None -> NotUsed "unusual Compile items"
            | Some _ when hasDisallowedFiles solutionDirectory -> NotUsed "unusual files in the solution folder"
            | Some files ->
                let runDirectory = prepareRunDirectory shapeDirectory projectName
                let arguments = compilerArguments shapeDirectory runDirectory projectName (rootNamespace projectFileText projectName) files
                let responseFile = Path.Combine(runDirectory, "fsc.rsp")
                File.WriteAllLines(responseFile, arguments)

                let exitCode, output = runCompiler shapeDirectory solutionDirectory responseFile
                let errorLines =
                    output.Split('\n')
                    |> Array.map (fun line -> line.TrimEnd('\r'))
                    |> Array.filter (fun line -> line.Contains(": error FS") || line.StartsWith("error FS"))

                match exitCode, errorLines with
                | 0, [||] -> Compiled(Path.Combine(runDirectory, projectName + ".dll"))
                | 0, _ -> NotUsed "compiler reported errors but succeeded"
                | _, [||] -> NotUsed "compiler failed without reporting errors"
                | _, lines when not (lines |> Array.forall (fun (line: string) -> errorLine.IsMatch line)) -> NotUsed "compiler errors without a location"
                | _, lines -> CompileErrors(lines |> Array.map (fun line -> $"{line} [{Path.GetFullPath(context.ProjectFile)}]"))
