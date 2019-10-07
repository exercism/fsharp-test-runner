namespace Exercism.TestRunner.FSharp.IntegrationTests
{
    internal class TestRun
    {
        public string Expected { get; }
        public string Actual { get; }

        public TestRun(string expected, string actual) =>
            (Expected, Actual) = (expected, actual);
    }
}