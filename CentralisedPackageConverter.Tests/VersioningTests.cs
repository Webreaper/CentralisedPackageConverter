using System.Xml.Linq;
using FluentAssertions;
using NuGet.Versioning;

namespace CentralisedPackageConverter.Tests;

public class VersioningTests : FileTestsBase
{
    private const string ProjectFileTemplate = "Test{0}.csproj";

    /// <summary>
    /// Created project files, full paths, to be stored here.
    /// </summary>
    private readonly List<string> _projectFilePaths = new();


    [SetUp]
    public void SetUp()
    {
        InitTestFileSettings();
    }


    [Test]
    [TestCase(false, Description = "Max version")]
    [TestCase(true, Description = "Min version")]
    public void VersioningPickRegular(bool pickLowest)
    {
        var initialProjectContent =
            @"<Project>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""0.0.0"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""5.3.10"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""2.9.92"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""10.1.11"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""10.1.10"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""10.0.11"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        var expectedVersion = pickLowest
            ? "0.0.0"
            : "10.1.11";
        TestResultVersion(new [] { initialProjectContent }, expectedVersion, pickLowest);
        CheckProjectFilesHaveVersionsRemoved();
    }


    [Test]
    [TestCase(false, Description = "Max version")]
    [TestCase(true, Description = "Min version")]
    public void VersioningPickRegularMultipleFiles(bool pickLowest)
    {
        var initialProjectContentTemplate =
            @"<Project>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""{0}"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        var versions = new[]
        {
            "0.0.0", "5.3.10", "2.9.92", "10.1.11", "10.1.10", "10.0.11"
        };
        var initialProjectContents = versions.Select(v => string.Format(initialProjectContentTemplate, v))
            .ToList();

        var expectedVersion = pickLowest
            ? "0.0.0"
            : "10.1.11";

        TestResultVersion(initialProjectContents, expectedVersion, pickLowest);
        CheckProjectFilesHaveVersionsRemoved();
    }


    [Test]
    [TestCase(VersionComparison.Default, "2.0.0", Description = "Default, release in comparison: prefer regular version over prerelease")]
    [TestCase(VersionComparison.Version, "2.0.0-prerelease.1", Description = "Version only in comparison: regular and prerelease equal (pick first).")]
    [TestCase(VersionComparison.VersionRelease, "2.0.0", Description = "Release in comparison: prefer regular version over prerelease")]
    public void VersioningByPrerelease(VersionComparison comparison, string expectedVersion)
    {
        var initialProjectContent =
            @"<Project>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.0.0"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""2.0.0-prerelease.1"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""2.0.0-prerelease.2"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""2.0.0"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestResultVersion(new[] { initialProjectContent }, expectedVersion, false, default, comparison);
        CheckProjectFilesHaveVersionsRemoved();
    }

    [Test]
    [TestCase(VersionComparison.Default, "2.0.0-prerelease.1", Description = "Ignore version metadata (pick first).")]
    [TestCase(VersionComparison.Version, "2.0.0-prerelease.1", Description = "Ignore version metadata (pick first).")]
    [TestCase(VersionComparison.VersionRelease, "2.0.0-prerelease.1", Description = "Ignore version metadata (pick first).")]
    [TestCase(VersionComparison.VersionReleaseMetadata, "2.0.0-prerelease.1+metadata.2", Description = "By version metadata, highest.")]
    public void VersioningByMetadata(VersionComparison comparison, string expectedVersion)
    {
        var initialProjectContent =
            @"<Project>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""2.0.0-prerelease.1"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""2.0.0-prerelease.1+metadata.1"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""2.0.0-prerelease.1+metadata.2"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestResultVersion(new[] { initialProjectContent }, expectedVersion, false, default, comparison);
        CheckProjectFilesHaveVersionsRemoved();
    }

    [Test]
    [TestCase(false, "4.0.0-prerelease+metadata")]
    [TestCase(true, "3.0.0+metadata")]
    public void VersioningIgnorePrerelease(bool ignorePrerelease, string expectedVersion)
    {
        var initialProjectContent =
            @"<Project>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.0.0"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""2.0.0-prerelease"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""3.0.0+metadata"" />" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""4.0.0-prerelease+metadata"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestResultVersion(new[] { initialProjectContent }, expectedVersion, false, ignorePrerelease);
        CheckProjectFilesHaveVersionsRemoved();
    }


    [Test]
    public void TestVariableAsVersion()
    {
        var initialProjectContent =
            @"<Project>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""$(VersionVariable)"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestResultVersion([initialProjectContent], "$(VersionVariable)");
        CheckProjectFilesHaveVersionsRemoved();
    }



    // something like this generator https://github.com/belav/csharpier/tree/master/Src/CSharpier.Tests.Generators
    // to create the tests that copying files to a temp directory out of the folder structure in here
    // and then do the similar steps to this method
    private void TestResultVersion(
        IEnumerable<string> initialProjectContents,
        string expectedVersionString, 
        bool pickLowest = default,
        bool ignorePrerelease = default,
        VersionComparison comparison = default)
    {
        var packageConverter = new PackageConverter();

        var counter = 0;
        foreach (var initialProjectContent in initialProjectContents)
        {
            counter++;
            var directory = Path.Combine(this.TestDirectoryInfo.FullName, $"Test{counter}");
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, string.Format(ProjectFileTemplate, counter));
            _projectFilePaths.Add(filePath);
            WriteAllText(filePath, initialProjectContent, null);
        }

        var options = new CommandLineOptions
        {
            RootDirectory = TestDirectoryInfo.FullName,
            Force = true,
            PickMinVersion = pickLowest,
            IgnorePrerelease = ignorePrerelease,
            VersionComparison = comparison,
        };
        packageConverter.ProcessConversion(options);

        var packagesContent = ReadAllText(this.PackagesFilePath, null);
        var packagesXml = XDocument.Parse(packagesContent);
        packagesXml.Root.Should().NotBeNull("Root XML element <Project>... required" + packagesContent);
        var itemGroup = GetSingleXElement(packagesXml.Root.Elements(), "ItemGroup", packagesContent);
        var packageVersion = GetSingleXElement(itemGroup.Elements(), "PackageVersion", packagesContent);
        var versionAttribute = GetSingleXAttribute(packageVersion.Attributes(), "Version", packagesContent);
        versionAttribute.Value.Should().NotBeNullOrEmpty("'Version' value is required" + packagesContent)
            .And.Be(expectedVersionString, "Must be the expected 'Version'" + packagesContent);
    }

    /// <remarks>
    /// This may need to change, if certain versions are to be preserved (e.g. unused floating, prerelease).
    /// </remarks>>
    private void CheckProjectFilesHaveVersionsRemoved()
    {
        foreach (var projectFilePath in _projectFilePaths)
        {
            var projectContent = ReadAllText(projectFilePath, null);
            var projectXml = XDocument.Parse(projectContent);
            var packageReferences = projectXml.Descendants("PackageReference");
            foreach (var projectReference in packageReferences)
            {
                var versionAttributesElements =
                    projectReference.Attributes("Version").Cast<XObject>()
                        .Concat(projectReference.Elements("Version"));
                versionAttributesElements.Any().Should().BeFalse(
                    "Version attributes/elements should be removed from:{0}{1}{0}",
                    LineWrap, projectReference.ToString());
            }
        }
    }


    private static XElement GetSingleXElement(IEnumerable<XElement>? xElements, string elementName, string assertedDocument)
    {
        var xElementsList = xElements
            ?.Where(xe => StringComparer.Ordinal.Equals(elementName, xe.Name.LocalName))
            .ToList();
        xElementsList.Should().NotBeNull("List with XElement '{1}' required {0}{2}{0}", LineWrap, elementName, assertedDocument);
        xElementsList.Count.Should().Be(1, "One '{1}' XElement required {0}{2}{0}", LineWrap, elementName, assertedDocument);
        return xElementsList.Single();
    }

    private static XAttribute GetSingleXAttribute(IEnumerable<XAttribute>? xAttributes, string attributeName, string assertedDocument)
    {
        var xAttributesList = xAttributes
            ?.Where(xa => StringComparer.Ordinal.Equals(attributeName, xa.Name.LocalName))
            .ToList();
        xAttributesList.Should().NotBeNull("List with XAttribute '{1}' required {0}{2}{0}", LineWrap, attributeName, assertedDocument);
        xAttributesList.Count.Should().Be(1, "One '{1}' XAttribute required {0}{2}{0}", LineWrap, attributeName, assertedDocument);
        return xAttributesList.Single();
    }
}