using System.Text;

namespace CentralisedPackageConverter.Tests;

using FluentAssertions;

public class PackageConverterTests
{
    private const string ProjectFile = "Test.csproj";
    private const string PackagesConfigFile = "Directory.Packages.props";

    private static readonly string LineWrap = Environment.NewLine;

    private static readonly string CustomLineWrap = 
        LineWrap == "\r\n" ? "\n" : "\r\n";

    /// <summary>
    /// UTF-32, not commonly a default encoding.
    /// </summary>
    private static readonly Encoding CustomEncoding = Encoding.UTF32;

    private DirectoryInfo? _testDirectoryInfo;

    private DirectoryInfo TestDirectoryInfo => _testDirectoryInfo ??
                                               throw new InvalidOperationException(
                                                   $"Backing field {nameof(_testDirectoryInfo)} has not been initialized with value.");

    private string? _projectFilePath;

    private string ProjectFilePath => _projectFilePath ??
                                      throw new InvalidOperationException(
                                          $"Backing field {nameof(_projectFilePath)} has not been initialized with value.");

    private string? _packagesFilePath;

    private string PackagesFilePath => _packagesFilePath ??
                                       throw new InvalidOperationException(
                                           $"Backing field {nameof(_packagesFilePath)} has not been initialized with value.");

    [SetUp]
    public void SetUp()
    {
        // TODO check if it supports linux correctly (should do, temp folder likely not hard drive)
        // TODO setup a github action

        var testTimestampBuilder = new StringBuilder(DateTime.Now.ToString("s"))
            .Replace("-", string.Empty)
            .Replace(":", string.Empty);
        var currentTestDirPrefix = "CentralisedPackageConverter_" + 
                                   TestContext.CurrentContext.Test.ClassName + "_" +
                                   TestContext.CurrentContext.Test.Name + "_" +
                                   testTimestampBuilder + "_";
        _testDirectoryInfo = Directory.CreateTempSubdirectory(currentTestDirPrefix);
        foreach (var directoryInfo in TestDirectoryInfo.EnumerateDirectories())
        {
            directoryInfo.Delete(true);
        }

        _projectFilePath = Path.Combine(TestDirectoryInfo.FullName, ProjectFile);
        _packagesFilePath = Path.Combine(TestDirectoryInfo.FullName, PackagesConfigFile);
    }

    [Test]
    public void BasicPackageWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent
        );
    }

    [Test]
    public void CaseInsensitiveVersionAttrWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap + 
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent
        );
    }

    [Test]
    public void BasicPackageWithRootNamespaceWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap + 
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent
        );
    }

    [Test]
    public void BasicPackageWithVersionElementWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"">" + LineWrap +
            @"      <Version>1.2.3</Version>" + LineWrap +
            @"    </PackageReference>" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"">" + LineWrap +
            @"    </PackageReference>" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent
        );
    }

    [Test]
    public void BasicPackageWithConditionWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent
        );
    }

    [Test]
    public void DuplicatedPackageWithConditionWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""4.5.6"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""4.5.6"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent
        );
    }

    [Test]
    public void TransitivePinningWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>" + LineWrap + // =true
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent,
            true
        );
    }

    [Test]
    public void CustomLineWrapsWork()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + CustomLineWrap +
            @"  <ItemGroup>" + CustomLineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + CustomLineWrap +
            @"  </ItemGroup>" + CustomLineWrap +
            @"</Project>";
        var expectedPackageContent =
            @"<Project>" + CustomLineWrap +
            @"  <PropertyGroup>" + CustomLineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + CustomLineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + CustomLineWrap +
            @"  </PropertyGroup>" + CustomLineWrap +
            @"  <ItemGroup>" + CustomLineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + CustomLineWrap +
            @"  </ItemGroup>" + CustomLineWrap +
            @"</Project>" + CustomLineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent,
            false, 
            null, 
            CustomLineWrap);
    }

    [Test]
    public void DefaultEncodingWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage_äöüß"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage_äöüß"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageVersion Include=""TestPackage_äöüß"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent);

        // Byte order mark, file binaries may start with BOM.
        var defaultBom = Encoding.Default.GetPreamble();

        var defaultEncodingProjectBinary = defaultBom
            .Concat(Encoding.Default.GetBytes(expectedProjectContent))
            .ToArray();
        var writtenEncodingProjectBinary = File.ReadAllBytes(ProjectFilePath);
        AssertAreBinariesEqual(defaultEncodingProjectBinary, writtenEncodingProjectBinary, "Same encodings for project file?");

        var defaultEncodingPackagesBinary = defaultBom
            .Concat(Encoding.Default.GetBytes(expectedPackageContent))
            .ToArray();
        var writtenEncodingPackagesBinary = File.ReadAllBytes(PackagesFilePath);
        AssertAreBinariesEqual(defaultEncodingPackagesBinary, writtenEncodingPackagesBinary, "Same encodings for packages file?");
    }


    [Test]
    public void CustomEncodingWorks()
    {
        Assert.AreNotEqual(CustomEncoding, Encoding.Default, 
            $"Is custom encoding {CustomEncoding.WebName} different than default {Encoding.Default}?");

        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage_äöüß"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage_äöüß"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageVersion Include=""TestPackage_äöüß"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent,
            false,
            CustomEncoding.WebName);

        // Byte order marks, file binaries may start with BOM.
        var defaultBom = Encoding.Default.GetPreamble();
        var customBom = CustomEncoding.GetPreamble();

        var defaultEncodingProjectBinary = defaultBom
            .Concat(Encoding.Default.GetBytes(expectedProjectContent))
            .ToArray();
        var customEncodingProjectBinary = customBom
            .Concat(CustomEncoding.GetBytes(expectedProjectContent))
            .ToArray();
        var projectFileBinary = File.ReadAllBytes(ProjectFilePath);
        AssertAreBinariesNotEqual(defaultEncodingProjectBinary, projectFileBinary, "Different encodings for project file?");
        AssertAreBinariesEqual(customEncodingProjectBinary, projectFileBinary, "Is project file binary expected custom encoding binary?");

        var defaultEncodingPackagesBinary = defaultBom
            .Concat(Encoding.Default.GetBytes(expectedPackageContent))
            .ToArray();
        var customEncodingPackagesBinary = customBom
            .Concat(CustomEncoding.GetBytes(expectedPackageContent))
            .ToArray();
        var packagesFileBinary = File.ReadAllBytes(PackagesFilePath);
        AssertAreBinariesNotEqual(defaultEncodingPackagesBinary, packagesFileBinary, "Different encodings for packages file?");
        AssertAreBinariesEqual(customEncodingPackagesBinary, packagesFileBinary, "Is packages file binary expected custom encoding binary?");
    }


    // TODO these could all be cleaner if the tests were just files on disk
    // something like this generator https://github.com/belav/csharpier/tree/master/Src/CSharpier.Tests.Generators
    // to create the tests that copying files to a temp directory out of the folder structure in here
    // and then do the similar steps to this method
    private void TestWithSingleProject(
        string initialProjectContent,
        string expectedProjectContent,
        string expectedPackageContent,
        bool transitivePinning = false,
        string? encodingWebName = null,
        string? lineWrap = null
    )
    {
        var packageConverter = new PackageConverter();
        //var testDirectory = Path.Combine(this.FixtureDirectory, DateTime.Now.Ticks.ToString());
        //Directory.CreateDirectory(testDirectory);
        //var csProjPath = Path.Combine(testDirectory, ProjectFile);
        WriteAllText(this.ProjectFilePath, initialProjectContent, encodingWebName);

        var options = new CommandLineOptions
        {
            RootDirectory = TestDirectoryInfo.FullName,
            Force = true,
            TransitivePinning = transitivePinning,
            EncodingWebName = encodingWebName,
            LineWrap = lineWrap?.Replace("\r", "cr").Replace("\n", "lf")
        };
        packageConverter.ProcessConversion(options);

        var newCsProjContent = ReadAllText(this.ProjectFilePath, encodingWebName);
        newCsProjContent.Should().Be(expectedProjectContent);
        var packagesContent = ReadAllText(this.PackagesFilePath, encodingWebName);
        packagesContent.Should().Be(expectedPackageContent);
    }

    [Test]
    public void BasicRevertWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var initialPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestRevertWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            initialPackageContent
        );
    }

    [Test]
    public void BasicRevertWithRootNamespaceWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var initialPackageContent =
            @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup>" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestRevertWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            initialPackageContent
        );
    }

    [Test]
    public void BasicRevertWithConditionWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">" + LineWrap +
            @"    <PackageReference Include=""TestPackage"" Version=""4.5.6"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>";
        var initialPackageContent =
            @"<Project>" + LineWrap +
            @"  <PropertyGroup>" + LineWrap +
            @"    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>" + LineWrap +
            @"    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>" + LineWrap +
            @"  </PropertyGroup>" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">" + LineWrap +
            @"    <PackageVersion Include=""TestPackage"" Version=""4.5.6"" />" + LineWrap +
            @"  </ItemGroup>" + LineWrap +
            @"</Project>" + LineWrap;

        TestRevertWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            initialPackageContent
        );
    }


    private void TestRevertWithSingleProject(
        string initialProjectContent,
        string expectedProjectContent,
        string initialPackageContent,
        bool transitivePinning = false,
        string? encodingWebName = null
    )
    {
        var packageConverter = new PackageConverter();
        //var testDirectory = Path.Combine(this.FixtureDirectory, DateTime.Now.Ticks.ToString());
        //Directory.CreateDirectory(testDirectory);
        //var csProjPath = Path.Combine(testDirectory, "Test.csproj");
        WriteAllText(ProjectFilePath, initialProjectContent, encodingWebName);
        WriteAllText(PackagesFilePath, initialPackageContent, encodingWebName);

        var options = new CommandLineOptions
        {
            RootDirectory = TestDirectoryInfo.FullName,
            Revert = true,
            Force = true,
            TransitivePinning = transitivePinning,
            EncodingWebName = encodingWebName
        };
        packageConverter.ProcessConversion(options);

        var newCsProjContent = ReadAllText(ProjectFilePath, encodingWebName);
        newCsProjContent.Should().Be(expectedProjectContent);
        File.Exists(PackagesFilePath).Should().BeFalse();
    }


    /// <summary>
    /// <see cref="File.ReadAllText(string, Encoding)"/> or
    /// <see cref="File.ReadAllText(string)"/> if no encoding
    /// </summary>
    private static string ReadAllText(string path, string? encodingWebName)
    {
        return encodingWebName is null
            ? File.ReadAllText(path)
            : File.ReadAllText(path, Encoding.GetEncoding(encodingWebName));
    }

    /// <summary>
    /// <see cref="File.WriteAllText(string, string?, Encoding)"/> or
    /// <see cref="File.WriteAllText(string, string?)"/> if no encoding
    /// </summary>
    private static void WriteAllText(string path, string? contents, string? encodingWebName)
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

    private static void AssertAreBinariesEqual(byte[] binary1, byte[] binary2, string? description)
    {
        Assert.AreEqual(binary1.Length, binary2.Length, 
            "Binary lengths are not equal. " + description + GetBinariesString(binary1, binary2));
        for (int i = 0; i < binary1.Length; i++)
        {
            Assert.AreEqual(binary1[i], binary2[i], 
                $"Binary byte values at position {i} not equal. " + description + GetBinariesString(binary1, binary2));
        }
    }

    private static void AssertAreBinariesNotEqual(byte[] binary1, byte[] binary2, string? description)
    {
        if (binary1.Length != binary2.Length)
        {
            return;
        }

        if (Enumerable.Range(0, binary1.Length).Any(i => binary1[i] != binary2[i]))
        {
            return;
        }
        Assert.Fail("Binaries are equal. " + description + GetBinariesString(binary1, binary2));
    }

    private static string GetBinariesString(byte[] binary1, byte[] binary2)
    {
        return Environment.NewLine +
               "Binary 1: " + Environment.NewLine +
               string.Join(",", binary1.Select(b => b.ToString("X"))) + Environment.NewLine +
               "Binary 2: " + Environment.NewLine +
               string.Join(",", binary2.Select(b => b.ToString("X"))) + Environment.NewLine;
    }
}
