using System.Text;

namespace CentralisedPackageConverter.Tests;

public abstract class FileTestsBase
{
    internal const string ProjectFile = "Test.csproj";
    internal const string PackagesConfigFile = "Directory.Packages.props";

    protected static readonly string LineWrap = Environment.NewLine;

    private DirectoryInfo? _testDirectoryInfo;

    protected DirectoryInfo TestDirectoryInfo => _testDirectoryInfo ??
                                                 throw new InvalidOperationException(
                                                     $"Backing field {nameof(_testDirectoryInfo)} has not been initialized with value.");

    private string? _projectFilePath;

    /// <summary>
    /// Default project file in test root directory.
    /// </summary>
    protected string ProjectFilePath => _projectFilePath ??
                                        throw new InvalidOperationException(
                                            $"Backing field {nameof(_projectFilePath)} has not been initialized with value.");

    private string? _packagesFilePath;

    /// <summary>
    /// Directory.Packages.props file path, in test root directory.
    /// </summary>
    protected string PackagesFilePath => _packagesFilePath ??
                                         throw new InvalidOperationException(
                                             $"Backing field {nameof(_packagesFilePath)} has not been initialized with value.");

    protected FileTestsBase()
    { }


    protected virtual void InitTestFileSettings()
    {
        // TODO check if it supports linux correctly (should do, temp folder likely not hard drive)
        // TODO setup a github action

        var testTimestampBuilder = new StringBuilder(DateTime.Now.ToString("s")) // like 'O' without fractional seconds
            .Replace("-", string.Empty)
            .Replace(":", string.Empty);
        var currentTestDirPrefix = "CentralisedPackageConverter_" +
                                   TestContext.CurrentContext.Test.ClassName + "_" +
                                   TestContext.CurrentContext.Test.Name.Replace("\"", "_") + "_" + // " occurs in parameterized tests
                                   testTimestampBuilder + "_";
        _testDirectoryInfo = Directory.CreateTempSubdirectory(currentTestDirPrefix);
        foreach (var directoryInfo in TestDirectoryInfo.EnumerateDirectories())
        {
            directoryInfo.Delete(true);
        }

        _projectFilePath = Path.Combine(TestDirectoryInfo.FullName, ProjectFile);
        _packagesFilePath = Path.Combine(TestDirectoryInfo.FullName, PackagesConfigFile);
    }


    /// <summary>
    /// <see cref="File.ReadAllText(string, Encoding)"/> or
    /// <see cref="File.ReadAllText(string)"/> if no encoding
    /// </summary>
    protected static string ReadAllText(string path, string? encodingWebName)
    {
        return encodingWebName is null
            ? File.ReadAllText(path)
            : File.ReadAllText(path, Encoding.GetEncoding(encodingWebName));
    }

    /// <summary>
    /// <see cref="File.WriteAllText(string, string?, Encoding)"/> or
    /// <see cref="File.WriteAllText(string, string?)"/> if no encoding
    /// </summary>
    protected static void WriteAllText(string path, string? contents, string? encodingWebName)
    {
        if (encodingWebName != null)
        {
            File.WriteAllText(path, contents, Encoding.GetEncoding(encodingWebName));
        }
        else
        {
            File.WriteAllText(path, contents);
        }
    }
}