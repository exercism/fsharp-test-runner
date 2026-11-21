#!/usr/bin/env dotnet

// File-based apps turn on native AOT by default, which makes restore download the AOT compiler and
// runtime packs. This app only runs with `dotnet run`, so turning AOT off lets it run offline.
#:property PublishAot=false

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

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("usage: bin/generate-shapes.cs <out_dir> [path to the exercism/fsharp repo]");
    return 1;
}

var outDirectory = args[0];
var trackDirectory = args.Length > 1 ? args[1] : "../fsharp";
HashSet<string> ignoredProperties = ["IsPackable", "RootNamespace"];

// Start from the first commit that mentions xunit.v3, the track's switch to xUnit v3.
var xunitV3Commits = Lines(Git("log", "--format=%H", "-S", "xunit.v3", "--", "exercises"));
var since = xunitV3Commits.Length > 0 ? xunitV3Commits[^1] : "HEAD";
var commits = Lines(Git("log", "--format=%H", $"{since}~1..HEAD", "--", "exercises")).Append("HEAD");

// Each shape keeps the first project file found for it, and the exercises that use it.
var shapes = new Dictionary<string, (Node Key, string Text, HashSet<string> Exercises)>();

using (var catFile = new GitCatFile(trackDirectory))
{
    foreach (var commit in commits)
    {
        foreach (var path in Git("ls-tree", "-r", "--name-only", commit, "exercises").Split('\n'))
        {
            if (!path.EndsWith(".fsproj", StringComparison.Ordinal))
                continue;

            var text = catFile.Read($"{commit}:{path}").Replace("net9.0", "net10.0");
            if (!text.Contains("xunit.v3", StringComparison.Ordinal))
                continue;

            var key = Key(text);
            var keyText = key.ToString();
            if (!shapes.ContainsKey(keyText))
                shapes[keyText] = (key, text, []);
            shapes[keyText].Exercises.Add(path.Split('/')[2]);
        }
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

static string[] Lines(string text) => text.Split((char[])['\n', ' '], StringSplitOptions.RemoveEmptyEntries);

string Git(params string[] arguments)
{
    var startInfo = new ProcessStartInfo("git") { RedirectStandardOutput = true };
    startInfo.ArgumentList.Add("-C");
    startInfo.ArgumentList.Add(trackDirectory);
    foreach (var argument in arguments)
        startInfo.ArgumentList.Add(argument);

    using var process = Process.Start(startInfo)!;
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed with exit code {process.ExitCode}");
    return output;
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

// Reads files from the track repo through one long-running `git cat-file --batch` process,
// which is much faster than starting `git show` for every file in every commit.
sealed class GitCatFile : IDisposable
{
    private readonly Process process;

    public GitCatFile(string trackDirectory)
    {
        var startInfo = new ProcessStartInfo("git") { RedirectStandardInput = true, RedirectStandardOutput = true };
        foreach (var argument in new[] { "-C", trackDirectory, "cat-file", "--batch" })
            startInfo.ArgumentList.Add(argument);
        process = Process.Start(startInfo)!;
    }

    // Returns the file as text, with Windows and old Mac line endings turned into \n.
    public string Read(string objectName)
    {
        process.StandardInput.Write(objectName + "\n");
        process.StandardInput.Flush();

        var output = process.StandardOutput.BaseStream;
        var header = ReadLine(output).Split(' ');
        if (header.Length != 3)
            throw new InvalidOperationException($"git cat-file could not read {objectName}");

        var content = new byte[int.Parse(header[2])];
        output.ReadExactly(content);
        output.ReadByte(); // The newline after the content.

        var text = new UTF8Encoding(false, true).GetString(content);
        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private static string ReadLine(Stream stream)
    {
        var bytes = new List<byte>();
        for (var b = stream.ReadByte(); b != '\n'; b = stream.ReadByte())
        {
            if (b == -1)
                throw new EndOfStreamException();
            bytes.Add((byte)b);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    public void Dispose()
    {
        process.StandardInput.Close();
        process.WaitForExit();
        process.Dispose();
    }
}
