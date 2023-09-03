﻿using System.Text;
using System.Xml.Linq;

namespace CentralisedPackageConverter;

public class PackageConverter
{
    private readonly Dictionary<string, Dictionary<string, string>> referencesByConditionThenName = new(StringComparer.OrdinalIgnoreCase);
    private const string s_DirPackageProps = "Directory.Packages.props";
    private const string s_FsProjExtension = ".fsproj";

    private const string s_FsharpLegacyFrameworkCondition =
        "$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net470'))";

    private static readonly HashSet<string> s_extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csproj",
        ".vbproj",
        s_FsProjExtension,
        ".props",
        ".targets"
    };

    private static readonly XElement s_fsharpCorePackageReference = XElement.Parse($"""
  <ItemGroup>
    <PackageReference Include="FSharp.Core" />
    <PackageReference Include="System.ValueTuple" Condition="{s_FsharpLegacyFrameworkCondition}" />
  </ItemGroup>
""", LoadOptions.PreserveWhitespace);

    public void ProcessConversion(CommandLineOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.RootDirectory))
        {
            Console.WriteLine("Root directory argument must not be empty.");
            return;
        }

        var encoding = Formatting.GetCommonEncoding(o.EncodingWebName);
        var linewrap = Formatting.GetLineWrap(o.LineWrap);

        var packageConfigPath = Path.Combine(o.RootDirectory, s_DirPackageProps);

        if (o.DryRun)
            Console.WriteLine("Dry run enabled - no changes will be made on disk.");

        var rootDir = new DirectoryInfo(o.RootDirectory);

        // Find all the csproj files to process
        var projects = rootDir.GetFiles("*.*", SearchOption.AllDirectories)
                              .Where(x => s_extensions.Contains(x.Extension))
                              .Where(x => !x.Name.Equals(s_DirPackageProps))
                              .OrderBy(x => x.Name)
                              .ToList();

        if (!o.Force && !o.DryRun)
        {
            Console.WriteLine("WARNING: You are about to make changes to the following project files:");
            projects.ForEach(p => Console.WriteLine($" {p.Name}"));
            Console.WriteLine("Are you sure you want to continue? [y/n]");
            if (Console.ReadKey().Key != ConsoleKey.Y)
            {
                Console.WriteLine("Aborting...");
                return;
            }
        }
        
        if (o.Revert)
        {
            ReadDirectoryPackagePropsFile(packageConfigPath);
            
            projects.ForEach(proj => RevertProject(proj, o.DryRun, encoding, linewrap));

            if (!o.DryRun)
            {
                Console.WriteLine($"Deleting {packageConfigPath}...");
                File.Delete(packageConfigPath);
            }
        }
        else
        {
            if (o.Merge)
            {
                ReadDirectoryPackagePropsFile(packageConfigPath);
            }

            projects.ForEach(proj => ConvertProject(proj, o.DryRun, encoding, linewrap));

            if (this.referencesByConditionThenName.Any())
            {
                WriteDirectoryPackagesConfig(packageConfigPath, o.DryRun, o.TransitivePinning, encoding, linewrap);
            }
            else
                Console.WriteLine("No versioned references found in csproj files!");
        }
    }

    /// <summary>
    /// Revert a project to non-centralised package management
    /// by adding the versions back into the csproj file.
    /// </summary>
    /// <param name="project">Project file</param>
    /// <param name="dryRun">Test only</param>
    /// <param name="encoding">Use when writing file revert</param>
    /// <param name="lineWrap">Line wrap character(s) to use.</param>
    private void RevertProject(FileInfo project, bool dryRun, Encoding encoding, string lineWrap)
    {
        var xml = XDocument.Load(project.FullName);

        var packagesReferences = GetDescendants(xml, "PackageReference");

        var needToWriteChanges = false;

        foreach (var packageReference in packagesReferences )
        {
            if( packageReference.Parent is not null )
            {
                var condition = GetAttributeValue( packageReference.Parent, "Condition" ) ?? string.Empty;
                var package = GetAttributeValue(packageReference, "Include") ?? string.Empty;

                if( this.referencesByConditionThenName.TryGetValue( condition, out var packagesByName ) )
                {
                    if( packagesByName.TryGetValue( package, out var version ) )
                    {
                        packageReference.SetAttributeValue( "Version", version );
                        needToWriteChanges = true;
                    }
                    else
                    {
                        Console.WriteLine( $"No version found in {s_DirPackageProps} file for {package}! Skipping..." );
                    }
                }
                else
                {
                    Console.WriteLine( $"No condition found in {s_DirPackageProps} file for {condition}! Skipping..." );
                }
            }
            else
            {
                Console.WriteLine( $"Package reference does not have parent. Skipping..." );
            }
        }

        if (!dryRun && needToWriteChanges)
        {
            var xmlText = Formatting.FormatLineWraps(xml.ToString(), lineWrap);
            File.WriteAllText(project.FullName, xmlText, encoding);
        }
    }

    /// <summary>
    /// Read the list of references and versions from the Directory.Package.props file.
    /// </summary>
    /// <param name="packageConfigPath"></param>
    private void ReadDirectoryPackagePropsFile(string packageConfigPath)
    {
        var xml = XDocument.Load(packageConfigPath);

        var packageVersions = GetDescendants(xml, "PackageVersion");
        
        foreach (var packageVersion in packageVersions)
        {
            var package = GetAttributeValue(packageVersion, "Include") ?? "";
            var version = GetAttributeValue(packageVersion, "Version") ?? "";
            var condition = GetAttributeValue(packageVersion.Parent, "Condition") ?? string.Empty;

            if (!this.referencesByConditionThenName.TryGetValue(condition, out var packagesByName))
            {
                packagesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                this.referencesByConditionThenName[condition] = packagesByName;
            }
        
            packagesByName[package] = version;
        }
        
        Console.WriteLine($"Read {this.referencesByConditionThenName.Count} references from {packageConfigPath}");
    }

    /// <summary>
    /// Write the packages.config file.
    /// TODO: Would be good to read the existing file and merge if appropriate.
    /// </summary>
    private void WriteDirectoryPackagesConfig(string packageConfigPath, bool dryRun, bool transPin, Encoding encoding, string lineWrap)
    {
        var lines = new List<string>
        {
            "<Project>",
            "  <PropertyGroup>",
            "    " + new XElement("ManagePackageVersionsCentrally", true),
            "    " + new XElement("CentralPackageTransitivePinningEnabled", transPin),
            "  </PropertyGroup>"
        };

        foreach (var condition in this.referencesByConditionThenName.Keys)
        {
            if (string.IsNullOrEmpty(condition))
            {
                lines.Add("  <ItemGroup>");    
            }
            else
            {
                lines.Add($"  <ItemGroup Condition=\"{condition}\">");
            }

            foreach (var packageAndVersion in this.referencesByConditionThenName[condition].OrderBy(x => x.Key))
            {
                lines.Add($"    <PackageVersion Include=\"{packageAndVersion.Key}\" Version=\"{packageAndVersion.Value}\" />");
            }
            lines.Add("  </ItemGroup>");
        }

        lines.Add("</Project>");

        Console.WriteLine($"Writing {this.referencesByConditionThenName.Count} refs to {s_DirPackageProps} to {packageConfigPath}...");

        if (dryRun)
            lines.ForEach(x => Console.WriteLine(x));
        else
        {
            // if the file exists already (we are doing a merge with old file) - create a backup of the old file
            if (File.Exists(packageConfigPath))
            {
                File.Copy(packageConfigPath, System.IO.Path.ChangeExtension(packageConfigPath, "bak"));
            }
            File.WriteAllText(packageConfigPath, string.Concat(lines.Select(l => l + lineWrap)), encoding);
        }
    }

    /// <summary>
    /// Get descendants from an XML Document
    /// </summary>
    /// <param name="xml"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private IEnumerable<XElement> GetDescendants(XDocument xml, string name)
    {
        var rootNamespace = xml.Root?.GetDefaultNamespace() ?? XNamespace.None;

        return xml.Descendants(rootNamespace + name);
    }

    /// <summary>
    /// Safely get an attribute value from an XML Element, optionally
    /// deleting it after the value has been retrieved.
    /// </summary>
    /// <param name="elem">XML element. If null, result is also null.</param>
    /// <param name="name">Attribute name, checked namespace- and case-insensitive (pick first)</param>
    private static string? GetAttributeValue(XElement? elem, string name)
    {
        // Use case-insensitive attribute lookup
        var attr = elem?.Attributes().FirstOrDefault( x => 
            string.Equals( x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase ));

        return attr?.Value;
    }

    /// <summary>
    /// Safely delete attributes with a name.
    /// </summary>
    /// <param name="elem">XML element</param>
    /// <param name="name">Attribute name, checked namespace- and case-insensitive (may be multiple).</param>
    private static void RemoveAttributes(XElement elem, string name)
    {
        // Use case-insensitive attribute lookup
        var attr = elem.Attributes()
            .Where(x => string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))
            .ToList();

            attr.Remove();
    }

    /// <summary>
    /// Converts a csproj to Centrally managed packaging.
    /// </summary>
    private void ConvertProject(FileInfo projFile, bool dryRun, Encoding encoding, string lineWrap)
    {
        Console.WriteLine($"Processing references for {projFile.FullName}...");

        if (projFile.Extension == ".fsproj")
        {
            if (this.referencesByConditionThenName.TryGetValue("", out var referencesForCondition))
            {
                referencesForCondition.TryAdd("FSharp.Core", "$(FSCorePackageVersion)");
            }
            else
            {
                referencesForCondition =
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        { {"FSharp.Core", "$(FSCorePackageVersion)"} };
                this.referencesByConditionThenName[""] = referencesForCondition;
            }

            if (this.referencesByConditionThenName.TryGetValue(s_FsharpLegacyFrameworkCondition, out var condition))
            {
                condition.TryAdd("System.ValueTuple", "4.4.0");
            }
            else
            {
                condition =
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        { {"System.ValueTuple", "4.4.0"} };
                this.referencesByConditionThenName[s_FsharpLegacyFrameworkCondition] = condition;
            }
        }

        var xml = XDocument.Load(projFile.FullName, LoadOptions.PreserveWhitespace);

        var packageReferences = GetDescendants(xml, "PackageReference");

        var needToWriteChanges = false;

        var referencesToRemove = new List<XElement>();
        foreach (var packageReference in packageReferences)
        {
            var removeNodeIfEmpty = false;

            var package = GetAttributeValue(packageReference, "Include");

            if (string.IsNullOrEmpty(package))
            {
                package = GetAttributeValue(packageReference, "Update");
                removeNodeIfEmpty = true;
            }

            if (string.IsNullOrEmpty(package))
                continue;

            var version = GetAttributeValue(packageReference, "Version");
            RemoveAttributes(packageReference, "Version");

            if (string.IsNullOrEmpty(version))
            {
                var versionElement = packageReference.Descendants().FirstOrDefault();
                if (versionElement == null)
                {
                    continue;
                }
                
                version = versionElement.Value;
                if (versionElement.PreviousNode is XText textNode)
                {
                    textNode.Remove();
                }
                versionElement.Remove();
                if (!versionElement.HasElements)
                {
                    // TODO change to self closing?
                }
            }
            
            // If there is only an Update attribute left, and no child elements, then this node
            // isn't useful any more, so we can remove it entirely
            if (removeNodeIfEmpty && packageReference.Attributes().Count() == 1 && !packageReference.Elements().Any())
                referencesToRemove.Add(packageReference);

            needToWriteChanges = true;

            var itemGroup = packageReference.Parent;
            var condition = GetAttributeValue(itemGroup, "Condition") ?? string.Empty;

            if (!this.referencesByConditionThenName.TryGetValue(condition, out var referencesForCondition))
            {
                referencesForCondition = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                this.referencesByConditionThenName[condition] = referencesForCondition;
            }

            if (referencesForCondition.TryGetValue(package, out var existing))
            {
                // Existing reference for this package of same or greater version, so skip
                if (string.Compare(version, existing, StringComparison.Ordinal) >= 0)
                    continue;
            }

            Console.WriteLine($" Found new reference: {package} {version} with Condition {condition}");
            referencesForCondition[package] = version;
        }

        foreach (var reference in referencesToRemove)
        {
            reference.Remove();
        }

        if (projFile.Extension == ".fsproj")
        {
            xml.Root!.Add(s_fsharpCorePackageReference);
        }

        if (needToWriteChanges && !dryRun)
        {
            // this keeps the <xml element from appearing on the first line
            var xmlText = Formatting.FormatLineWraps(xml.ToString(), lineWrap);
            File.WriteAllText(projFile.FullName, xmlText, encoding);
        }
    }
}

