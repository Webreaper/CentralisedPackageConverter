﻿using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NuGet.Versioning;
using Spectre.Console;

namespace CentralisedPackageConverter;

public class PackageConverter
{
    private readonly Dictionary<string, Dictionary<string, PackageVersion>> referencesByConditionThenName = new(StringComparer.OrdinalIgnoreCase);
    private const string s_DirPackageProps = "Directory.Packages.props";

    internal static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase) {
        ".csproj",
        ".vbproj",
        ".fsproj",
        ".props",
        ".targets"
    };

    public void ProcessConversion(CommandLineOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.RootDirectory))
        {
            // This is never hit because of the CommandLineParser library marks it required.
            Output.ErrorLine("[maroon]Root directory argument must not be empty.[/]");
            return;
        }

        var encoding = Formatting.GetCommonEncoding(o.EncodingWebName);
        var linewrap = Formatting.GetLineWrap(o.LineWrap);
        var versioning = new Versioning(o.PickMinVersion, o.IgnorePrerelease, o.VersionComparison);
        var directoryCrawler = new DirectoryCrawler(o.ExcludeDirectoriesRegexString, AllowedFileExtensions);

        Output.TraceLine("Writing files with encoding: {0}", encoding.WebName);
        Output.TraceLine("Pick lowest version (not max): {0}", versioning.PickMinVersion);
        Output.TraceLine("{0}: {1}", nameof(VersionComparison), o.VersionComparison);

        var packageConfigPath = Path.Combine(o.RootDirectory, s_DirPackageProps);

        if (o.DryRun)
        {
            Output.InfoLine("Dry run enabled - no changes will be made on disk.");
        }

        if (File.Exists(o.RootDirectory))
        {
            Output.ErrorLine("Please specify a directory to scan instead of a file.");
            Output.TraceLine("Aborting...");
            return;
        }

        var rootDir = new DirectoryInfo(o.RootDirectory);

        // Find all the project files to process
        var projects = directoryCrawler.EnumerateRelevantFiles(rootDir, SearchOption.AllDirectories)
                              .Where(x => !x.Name.Equals(s_DirPackageProps))
                              .OrderBy(x => x.Name)
                              .ToList();

        if (!o.Force && !o.DryRun)
        {
            Output.WarningLine("WARNING: You are about to make changes to the following project files:");
            projects.ForEach(p => AnsiConsole.MarkupLine(" {0}", p.Name));
            Output.WarningLine("Are you sure you want to continue? [y/n]");
            if (Console.ReadKey().Key != ConsoleKey.Y)
            {
                Output.TraceLine("Aborting...");
                return;
            }
        }

        if (o.Revert)
        {
            ReadDirectoryPackagePropsFile(packageConfigPath);

            projects.ForEach(proj => RevertProject(proj, o.DryRun, encoding, linewrap));

            if (!o.DryRun)
            {
                Output.InfoLine("Deleting {0}...", packageConfigPath);
                File.Delete(packageConfigPath);
            }
        }
        else
        {
            if (o.Merge)
            {
                ReadDirectoryPackagePropsFile(packageConfigPath);
            }

            projects.ForEach(proj => ConvertProject(proj, o.DryRun, encoding, linewrap, versioning));

            if (this.referencesByConditionThenName.Any())
            {
                WriteDirectoryPackagesConfig(packageConfigPath, o.DryRun, o.TransitivePinning, encoding, linewrap);
            }
            else
            {
                Output.WarningLine("No versioned references found in project files!");
            }
        }
    }

    /// <summary>
    /// Revert a project to non-centralised package management
    /// by adding the versions back into the project file.
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

        foreach (var packageReference in packagesReferences)
        {
            if (packageReference.Parent is not null)
            {
                var condition = GetAttributeValue(packageReference.Parent, "Condition") ?? string.Empty;
                if (!TryGetAttributeValue(packageReference, "Include", out var package))
                {
                    if (!TryGetAttributeValue(packageReference, "Update", out package))
                    {
                        Output.TraceLine("Package reference has no Include or Update attribute. Skipping...");
                        continue;
                    }
                }

                if (this.referencesByConditionThenName.TryGetValue(condition, out var packagesByName))
                {
                    if (packagesByName.TryGetValue(package, out var version))
                    {
                        packageReference.SetAttributeValue("Version", version);
                        needToWriteChanges = true;
                    }
                    else
                    {
                        Output.TraceLine($"No version found in {s_DirPackageProps} file for {package}! Skipping...");
                    }
                }
                else
                {
                    Output.TraceLine($"No condition found in {s_DirPackageProps} file for {condition}! Skipping...");
                }
            }
            else
            {
                Output.TraceLine("Package reference does not have parent. Skipping...");
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
            var package = GetAttributeValue(packageVersion, "Include") ??
                          throw new InvalidOperationException("PackageVersion element has no Include");
            string? versionAttributeValue = GetAttributeValue(packageVersion, "Version");
            if (versionAttributeValue == null)
                throw new InvalidOperationException("PackageVersion element has no Version attribute");

            var version = PackageVersion.Parse(versionAttributeValue);
            var condition = GetAttributeValue(packageVersion.Parent, "Condition") ?? string.Empty;

            if (!this.referencesByConditionThenName.TryGetValue(condition, out var packagesByName))
            {
                packagesByName = new Dictionary<string, PackageVersion>(StringComparer.OrdinalIgnoreCase);
                this.referencesByConditionThenName[condition] = packagesByName;
            }

            packagesByName[package] = version;
        }

        Output.InfoLine("Read {0} references from {1}", this.referencesByConditionThenName.Count, packageConfigPath);
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

        foreach (var byConditionNames in this.referencesByConditionThenName)
        {
            var condition = byConditionNames.Key;
            var referencesByName = byConditionNames.Value;
            lines.Add(string.IsNullOrEmpty(condition)
                ? "  <ItemGroup>"
                : $"  <ItemGroup Condition=\"{condition}\">");

            foreach (var packageAndVersion in referencesByName.OrderBy(x => x.Key))
            {
                lines.Add($"    <PackageVersion Include=\"{packageAndVersion.Key}\" Version=\"{packageAndVersion.Value.ToFullString()}\" />");
            }
            lines.Add("  </ItemGroup>");
        }

        lines.Add("</Project>");

        Output.InfoLine($"Writing {this.referencesByConditionThenName.Count} refs to {s_DirPackageProps} to {packageConfigPath}...");

        if (dryRun)
            lines.ForEach(x => Output.TraceLine(x));
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
        var attr = elem?.Attributes().FirstOrDefault(x =>
            string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));

        return attr?.Value;
    }

    /// <summary>
    /// Try to get an attribute value from an XML Element
    /// </summary>
    /// <param name="elem">XML element. If null, result is false.</param>
    /// <param name="name">Attribute name, checked namespce- and case-insensitive (pick first)</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>Boolean whether attribute was picked</returns>
    private static bool TryGetAttributeValue(XElement? elem, string name, out string? value)
    {
        var attr = elem?.Attributes().FirstOrDefault(x =>
            string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));

        value = attr?.Value;
        return value != null;
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
    /// Converts a project file to Centrally managed packaging.
    /// </summary>
    private void ConvertProject(FileInfo projectFile, bool dryRun, Encoding encoding, string lineWrap, Versioning versioning)
    {
        Output.InfoLine("Processing references for [green]{0}[/]...", projectFile.FullName);

        var xml = XDocument.Load(projectFile.FullName, LoadOptions.PreserveWhitespace);

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

            var versionString = GetAttributeValue(packageReference, "Version");
            var version = default(PackageVersion?);
            if (PackageVersion.TryParse(versionString, out var parsedVersion))
            {
                version = parsedVersion;
            }
            else if (versionString != null)
            {
                Output.WarningLine("Can't parse Version attribute '{0}' in '{1}'",
                    versionString, packageReference);
            }
            RemoveAttributes(packageReference, "Version");

            if (version is null)
            {
                var versionElement = packageReference.Descendants().FirstOrDefault();
                if (versionElement == null)
                {
                    continue;
                }

                if (PackageVersion.TryParse(versionElement.Value, out parsedVersion))
                {
                    version = parsedVersion;
                }
                else if (string.IsNullOrEmpty(versionElement.Value))
                {
                    Output.WarningLine("Can't parse <Version... /> element value '{0}' in '{1}'",
                        versionElement.Value, packageReference);
                }
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
                referencesForCondition = new Dictionary<string, PackageVersion>(StringComparer.OrdinalIgnoreCase);
                this.referencesByConditionThenName[condition] = referencesForCondition;
            }

            if (referencesForCondition.TryGetValue(package, out var existing))
            {
                // Existing reference for this package of same or greater version, so skip
                if (versioning.PreferExisting(existing, version))
                {
                    continue;
                }
            }

            if (!versioning.IgnorePackageVersion(version))
            {
                if (condition != String.Empty)
                {
                    Output.InfoLine(" Found new reference: {0} {1} with Condition {2}",
                        package, version.ToFullString(), condition);
                }
                else
                {
                    Output.InfoLine(" Found new reference: {0} {1}",
                    package, version.ToFullString());
                }
                referencesForCondition[package] = version;
            }
            else
            {
                if (condition != String.Empty)
                {
                    Output.TraceLine("Ignoring {0} version {1} with condition {2}.", package, version?.ToFullString(), condition);
                }
                else
                {
                    Output.TraceLine("Ignoring {0} version {1}.", package, version?.ToFullString());
                }
            }
        }

        foreach (var reference in referencesToRemove)
        {
            reference.Remove();
        }

        if (needToWriteChanges && !dryRun)
        {
            // this keeps the <xml element from appearing on the first line
            var xmlText = Formatting.FormatLineWraps(xml.ToString(), lineWrap);
            File.WriteAllText(projectFile.FullName, xmlText, encoding);
        }
    }
}

