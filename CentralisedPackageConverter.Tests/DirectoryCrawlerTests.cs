using System.Text.RegularExpressions;
using FluentAssertions;

namespace CentralisedPackageConverter.Tests;

public class DirectoryCrawlerTests : FileTestsBase
{
    private static readonly DirectoryCrawler DefaultDirectoryCrawler = new(
        new Regex(CommandLineOptions.DefaultExcludeDirectories),
        PackageConverter.AllowedFileExtensions);

    [SetUp]
    public void SetUp()
    {
        InitTestFileSettings();
    }

    [Test]
    public void FindsAllRelevantFiles()
    {
        const int nbrOfDirs = 3;

        using (var stream = File.Create(Path.Combine(TestDirectoryInfo.FullName, "Directory.Build.props")))
        {
            stream.Close();
        }
        using (var stream = File.Create(Path.Combine(TestDirectoryInfo.FullName, "Directory.Packages.props")))
        {
            stream.Close();
        }

        var expectedProjectFiles = new List<string>();
        var currentDirectoryInfo = TestDirectoryInfo;
        for (int i = 1; i <= nbrOfDirs; i++)
        {
            currentDirectoryInfo = currentDirectoryInfo.CreateSubdirectory($"Test{i}"); // deeper structure to traverse
            var path = Path.Combine(currentDirectoryInfo.FullName, $"Test{i}.csproj");
            using (var stream = File.Create(path)) { stream.Close(); }
            expectedProjectFiles.Add(path);
        }

        var expectedFilePaths = new[]
            {
                Path.Combine(TestDirectoryInfo.FullName, "Directory.Build.props")
                // not Directory.Packages.props!
            }.Concat(expectedProjectFiles)
            .ToList();

        var foundFilePaths =
            DefaultDirectoryCrawler.EnumerateRelevantFiles(TestDirectoryInfo, SearchOption.AllDirectories)
                .Where(fi => fi.Name != "Directory.Packages.props")
                .Select(fi => fi.FullName).ToList();

        var filesExpectedFound = "Expected: " + LineWrap + string.Join(LineWrap, expectedFilePaths) + LineWrap +
                                 "Actual: " + LineWrap + string.Join(LineWrap, foundFilePaths);
        foundFilePaths.Count.Should().Be(expectedFilePaths.Count, "Correct file count? " + LineWrap +
                                                                  filesExpectedFound + LineWrap);
        for (int j = 0; j < expectedFilePaths.Count; j++)
        {
            foundFilePaths[j].Should().Be(expectedFilePaths[j], $"file #{j} shall be expected in found files." +
                                                                LineWrap + filesExpectedFound + LineWrap);
        }
    }


    [Test]
    public void NoFilesInExcludedDirectories()
    {
        var binDir = TestDirectoryInfo.CreateSubdirectory("bin");
        using (var stream = File.Create(Path.Combine(binDir.FullName, "bin.csproj")))
        {
            stream.Close();
        }

        var objDir = TestDirectoryInfo.CreateSubdirectory("obj");
        using (var stream = File.Create(Path.Combine(objDir.FullName, "obj.csproj")))
        {
            stream.Close();
        }

        var dotDir = TestDirectoryInfo.CreateSubdirectory(".dot");
        using (var stream = File.Create(Path.Combine(dotDir.FullName, "dot.csproj")))
        {
            stream.Close();
        }

        var foundFilePaths =
            DefaultDirectoryCrawler.EnumerateRelevantFiles(TestDirectoryInfo, SearchOption.AllDirectories)
                .Select(fi => fi.FullName).ToList();

        var filesExpectedFound = "Found: " + LineWrap + string.Join(LineWrap, foundFilePaths);
        foundFilePaths.Count.Should().Be(0, "No files found in excluded directories? " +
                                            LineWrap + filesExpectedFound + LineWrap);
    }
}