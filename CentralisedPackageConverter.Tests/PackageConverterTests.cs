namespace CentralisedPackageConverter.Tests;

using FluentAssertions;

public class PackageConverterTests
{
    private string fixtureDirectory;

    [SetUp]
    public void SetUp()
    {
        // TODO modify to support linux and setup a github action
        this.fixtureDirectory = "c:/temp/CentralisedPackageConverter";
        if (!Directory.Exists(this.fixtureDirectory))
        {
            Directory.CreateDirectory(this.fixtureDirectory);
        }
        foreach (var directory in Directory.EnumerateDirectories(this.fixtureDirectory))
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void BasicPackageWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""TestPackage"" />
  </ItemGroup>
</Project>";
        var expectedPackageContent =
            @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
</Project>
";

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
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""TestPackage"">
      <Version>1.2.3</Version>
    </PackageReference>
  </ItemGroup>
</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""TestPackage"">
    </PackageReference>
  </ItemGroup>
</Project>";
        var expectedPackageContent =
            @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
</Project>
";

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
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">
    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">
    <PackageReference Include=""TestPackage"" />
  </ItemGroup>
</Project>";

        var expectedPackageContent =
            @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">
    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
</Project>
";

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
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">
    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">
    <PackageReference Include=""TestPackage"" Version=""4.5.6"" />
  </ItemGroup>
</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">
    <PackageReference Include=""TestPackage"" />
  </ItemGroup>
  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">
    <PackageReference Include=""TestPackage"" />
  </ItemGroup>
</Project>";

        var expectedPackageContent =
            @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">
    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">
    <PackageVersion Include=""TestPackage"" Version=""4.5.6"" />
  </ItemGroup>
</Project>
";

        TestWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            expectedPackageContent
        );
    }

    // TODO these could all be cleaner if the tests were just files on disk
    // something like this generator https://github.com/belav/csharpier/tree/master/Src/CSharpier.Tests.Generators
    // to create the tests that copying files to a temp directory out of the folder structure in here
    // and then do the similar steps to this method
    private void TestWithSingleProject(
        string initialProjectContent,
        string expectedProjectContent,
        string expectedPackageContent
    )
    {
        var packageConverter = new PackageConverter();
        var testDirectory = Path.Combine(this.fixtureDirectory, DateTime.Now.Ticks.ToString());
        Directory.CreateDirectory(testDirectory);
        var csProjPath = Path.Combine(testDirectory, "Test.csproj");
        File.WriteAllText(csProjPath, initialProjectContent);

        packageConverter.ProcessConversion(testDirectory, false, false, true);

        var newCsProjContent = File.ReadAllText(csProjPath);
        newCsProjContent.Should().Be(expectedProjectContent);
        var packagesContent = File.ReadAllText(
            Path.Combine(testDirectory, "Directory.Packages.props")
        );
        packagesContent.Should().Be(expectedPackageContent);
    }

    [Test]
    public void BasicRevertWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">
    <PackageReference Include=""TestPackage"" />
  </ItemGroup>
  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">
    <PackageReference Include=""TestPackage"" />
  </ItemGroup>
</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">
    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">
    <PackageReference Include=""TestPackage"" Version=""4.5.6"" />
  </ItemGroup>
</Project>";
        var initialPackageContent =
            @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup Condition="" '$(TargetFramework)' == 'net6.0' "">
    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
  <ItemGroup Condition="" '$(TargetFramework)' == 'net48' "">
    <PackageVersion Include=""TestPackage"" Version=""4.5.6"" />
  </ItemGroup>
</Project>
";
    }

    [Test]
    public void BasicConditionRevertWorks()
    {
        var initialProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""TestPackage"" />
  </ItemGroup>
</Project>";

        var expectedProjectContent =
            @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
</Project>";
        var initialPackageContent =
            @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""TestPackage"" Version=""1.2.3"" />
  </ItemGroup>
</Project>
";

        TestRevertWithSingleProject(
            initialProjectContent,
            expectedProjectContent,
            initialPackageContent
        );
    }

    private void TestRevertWithSingleProject(
        string initialProjectContent,
        string expectedProjectContent,
        string initialPackageContent
    )
    {
        var packageConverter = new PackageConverter();
        var testDirectory = Path.Combine(this.fixtureDirectory, DateTime.Now.Ticks.ToString());
        Directory.CreateDirectory(testDirectory);
        var csProjPath = Path.Combine(testDirectory, "Test.csproj");
        File.WriteAllText(csProjPath, initialProjectContent);
        File.WriteAllText(
            Path.Combine(testDirectory, "Directory.Packages.props"),
            initialPackageContent
        );

        packageConverter.ProcessConversion(testDirectory, true, false, true);

        var newCsProjContent = File.ReadAllText(csProjPath);
        newCsProjContent.Should().Be(expectedProjectContent);
    }
}
