using System.IO;

namespace Exercism.TestRunner.FSharp.IntegrationTests
{
    internal class TestSolution
    {
        public string Slug { get; }
        public string Directory { get; }
        public string DirectoryName { get; }

        public TestSolution(string slug, string directory) =>
            (Slug, Directory, DirectoryName) = (slug, directory, Path.GetFileName(directory));
    }
}