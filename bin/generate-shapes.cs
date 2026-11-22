#!/usr/bin/env dotnet

// File-based apps turn on native AOT by default, which makes restore download the AOT compiler and
// runtime packs. This app only runs with `dotnet run`, so turning AOT off lets it run offline.
#:property PublishAot=false
#:package LibGit2Sharp@0.32.0

// Writes the numbered folders in shapes/ from the history of the exercism/fsharp track repo.
//
// A shape is a distinct xUnit v3 project file, ignoring the project name, its Compile items and the
// settings that don't affect compiling. This program reads every exercise's .fsproj in every commit since
// the track switched to xUnit v3, groups them by shape, and writes one template project file per shape to
// <out_dir>/<n>/ExercismShape.fsproj, with net9.0 replaced by net10.0 as the runner does. The Dockerfile
// builds each one (bin/build-shapes.sh), so that the fast path can reuse its compiler arguments.
//
// The grouping here is close to FastPath.shapeKey in the runner. If the two ever disagree, the worst case
// will be a duplicate template, or a project file with no matching template, which uses MSBuild.
//
// This is a file-based app, so it needs no project file. Usage, from the repo root:
//   bin/generate-shapes.cs shapes [path to the exercism/fsharp repo, default ../fsharp]

using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LibGit2Sharp;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("usage: bin/generate-shapes.cs <out_dir> [path to the exercism/fsharp repo]");
    return 1;
}

var outDirectory = args[0];
var trackDirectory = args.Length > 1 ? args[1] : "../fsharp";
HashSet<string> ignoredProperties = ["IsPackable", "RootNamespace"];

// Each shape keeps the first project file found for it, and the exercises that use it.
var shapes = new Dictionary<string, (Node Key, string Text, HashSet<string> Exercises)>();

using (var repository = new Repository(trackDirectory))
{
    // Read the commits newest first. Commits that leave the exercises folder as it was would find the
    // same files again, so they are skipped.
    var commits = repository.Commits
        .QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time })
        .Where(commit => commit["exercises"] is not null)
        .DistinctBy(commit => commit["exercises"].Target.Id);

    foreach (var commit in commits)
    {
        var found = false;
        foreach (var (path, blob) in ProjectFiles((Tree)commit["exercises"].Target, "exercises"))
        {
            var text = Text(blob).Replace("net9.0", "net10.0");
            if (!text.Contains("xunit.v3", StringComparison.Ordinal))
                continue;

            found = true;
            var key = Key(text);
            var keyText = key.ToString();
            if (!shapes.ContainsKey(keyText))
                shapes[keyText] = (key, text, []);
            shapes[keyText].Exercises.Add(path.Split('/')[2]);
        }

        // Commits from before the track switched to xUnit v3 have no xUnit v3 project files, so stop at the first one.
        if (!found)
            break;
    }
}

// Replace only the numbered folders, so that the shared files in the output folder stay.
if (Directory.Exists(outDirectory))
{
    foreach (var directory in Directory.EnumerateDirectories(outDirectory).Where(directory => Path.GetFileName(directory).All(char.IsAsciiDigit)))
        Directory.Delete(directory, recursive: true);
}

// The most used shapes come first. Shapes used by the same number of exercises are ordered by their key.
var ordered = shapes.Values
    .OrderByDescending(shape => shape.Exercises.Count)
    .ThenBy(shape => shape.Key.ToString(), StringComparer.Ordinal)
    .ToArray();
for (var i = 0; i < ordered.Length; i++)
{
    var directory = Path.Combine(outDirectory, i.ToString("00"));
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, "ExercismShape.fsproj"), Template(ordered[i].Text), new UTF8Encoding(false));
    Console.WriteLine($"{i} {ordered[i].Exercises.Count} {string.Join(", ", ordered[i].Exercises.Order(StringComparer.Ordinal).Take(3))}");
}

return 0;

// Reduces a project file to its properties and items (apart from Compile items), sorted, ignoring the
// properties that don't affect compiling.
Node Key(string text)
{
    var root = XElement.Parse(text.TrimStart('﻿'));
    var properties = new SortedDictionary<string, string>(StringComparer.Ordinal);
    var items = new List<Node>();
    foreach (var group in root.Elements())
    {
        foreach (var element in group.Elements())
        {
            if (group.Name == "PropertyGroup")
            {
                if (!ignoredProperties.Contains(element.Name.ToString()))
                    properties[element.Name.ToString()] = LeadingText(element);
            }
            else if (element.Name != "Compile")
            {
                var attributes = element.Attributes().Select(attribute => Node.Of(attribute.Name.ToString(), attribute.Value)).Order();
                var metadata = element.Elements().Select(child => Node.Of(child.Name.ToString(), LeadingText(child))).Order();
                items.Add(Node.Of(element.Name.ToString(), new Node(attributes.Concat(metadata).ToArray())));
            }
        }
    }

    return Node.Of(
        new Node(properties.Select(property => Node.Of(property.Key, property.Value)).ToArray()),
        new Node(items.Order().ToArray()));
}

// Keeps the project file as it is, with the Compile items replaced by the template's two files and no RootNamespace.
// The two files are shared by every template, so they sit in the shapes/ folder itself.
static string Template(string text)
{
    text = text.TrimStart('﻿').Replace("net9.0", "net10.0");
    text = new Regex("""(\s*<Compile Include="[^"]*" />)+""").Replace(text,
        "\n    <Compile Include=\"../ExercismShape.fs\" />\n    <Compile Include=\"../ExercismShapeTests.fs\" />", 1);
    return Regex.Replace(text, @"\s*<RootNamespace>[^<]*</RootNamespace>", "");
}

// The element's text before its first child element, trimmed.
static string LeadingText(XElement element) =>
    string.Concat(element.Nodes().TakeWhile(node => node is not XElement).OfType<XText>().Select(text => text.Value)).Trim();

// Every .fsproj file in the tree and its subfolders, in the order `git ls-tree -r` lists them.
static IEnumerable<(string Path, Blob Blob)> ProjectFiles(Tree tree, string path)
{
    foreach (var entry in tree)
    {
        var entryPath = $"{path}/{entry.Name}";
        if (entry.Target is Tree subtree)
        {
            foreach (var file in ProjectFiles(subtree, entryPath))
                yield return file;
        }
        else if (entry.Target is Blob blob && entry.Name.EndsWith(".fsproj", StringComparison.Ordinal))
            yield return (entryPath, blob);
    }
}

// Returns the file as text, with Windows and old Mac line endings turned into \n.
static string Text(Blob blob)
{
    using var stream = blob.GetContentStream();
    using var memory = new MemoryStream();
    stream.CopyTo(memory);
    var text = new UTF8Encoding(false, true).GetString(memory.ToArray());
    return text.Replace("\r\n", "\n").Replace('\r', '\n');
}

// A shape's key: a string, or a list of keys. Keys sort and print like Python tuples of strings, which
// keeps the order of the shapes the same as when the shapes were first generated.
sealed class Node : IComparable<Node>
{
    private readonly string? text;
    private readonly Node[]? children;

    public Node(string text) => this.text = text;

    public Node(Node[] children) => this.children = children;

    public static Node Of(params Node[] children) => new(children);

    public static implicit operator Node(string text) => new(text);

    public int CompareTo(Node? other)
    {
        if (text is not null)
            return string.CompareOrdinal(text, other!.text);

        for (var i = 0; i < Math.Min(children!.Length, other!.children!.Length); i++)
        {
            var comparison = children[i].CompareTo(other.children[i]);
            if (comparison != 0)
                return comparison;
        }
        return children.Length.CompareTo(other.children.Length);
    }

    public override string ToString()
    {
        if (text is not null)
            return Quote(text);

        return children!.Length == 1
            ? $"({children[0]},)"
            : $"({string.Join(", ", children.Select(child => child.ToString()))})";
    }

    // Quotes a string as Python's repr() does.
    private static string Quote(string value)
    {
        var quote = value.Contains('\'') && !value.Contains('"') ? '"' : '\'';
        var builder = new StringBuilder().Append(quote);
        foreach (var c in value)
        {
            builder.Append(c switch
            {
                '\\' => @"\\",
                '\n' => @"\n",
                '\r' => @"\r",
                '\t' => @"\t",
                _ when c == quote => $"\\{c}",
                _ when c < ' ' || c == '\x7f' => $"\\x{(int)c:x2}",
                _ => c.ToString()
            });
        }
        return builder.Append(quote).ToString();
    }
}
