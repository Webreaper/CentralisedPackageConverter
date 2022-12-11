using System;
using System.IO;
using System.Xml.Linq;

namespace CentralisedPackageConverter;

public class PackageConverter
{
    private readonly Dictionary<string, Dictionary<string, string>> referencesByConditionThenName = new(StringComparer.OrdinalIgnoreCase);
    private const string s_DirPackageProps = "Directory.Packages.props";

    private static readonly HashSet<string> s_extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csproj",
        ".vbproj",
        ".props",
        ".targets"
    };

    public void ProcessConversion(string solutionFolder, bool revert, bool dryRun, bool force)
    {
        var packageConfigPath = Path.Combine(solutionFolder, s_DirPackageProps);

        if (dryRun)
            Console.WriteLine("Dry run enabled - no changes will be made on disk.");

        var rootDir = new DirectoryInfo(solutionFolder);

        // Find all the csproj files to process
        var projects = rootDir.GetFiles("*.*", SearchOption.AllDirectories)
                              .Where(x => s_extensions.Contains(x.Extension))
                              .Where(x => !x.Name.Equals(s_DirPackageProps))
                              .OrderBy(x => x.Name)
                              .ToList();

        if (!force && !dryRun)
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
        
        if (revert)
        {
            ReadDirectoryPackagePropsFile(packageConfigPath);
            
            projects.ForEach(proj => RevertProject(proj, dryRun));

            if (!dryRun)
            {
                Console.WriteLine($"Deleting {packageConfigPath}...");
                File.Delete(packageConfigPath);
            }
        }
        else
        {
            projects.ForEach(proj => ConvertProject(proj, dryRun));

            if (this.referencesByConditionThenName.Any())
            {
                WriteDirectoryPackagesConfig(packageConfigPath, dryRun);
            }
            else
                Console.WriteLine("No versioned references found in csproj files!");
        }
    }

    /// <summary>
    /// Revert a project to non-centralised package management
    /// by adding the versions back into the csproj file.
    /// </summary>
    /// <param name="project"></param>
    /// <param name="dryRun"></param>
    private void RevertProject(FileInfo project, bool dryRun)
    {
        var xml = XDocument.Load(project.FullName);

        var packagesReferences = GetDescendants(xml, "PackageReference");

        var needToWriteChanges = false;

        foreach (var packageReference in packagesReferences)
        {
            var condition = GetAttributeValue(packageReference.Parent, "Condition", false) ?? string.Empty;
            var package = GetAttributeValue(packageReference, "Include", false);

            if (this.referencesByConditionThenName.TryGetValue(condition, out var packagesByName))
            {
                if (packagesByName.TryGetValue(package, out var version))
                {
                    packageReference.SetAttributeValue("Version", version);
                    needToWriteChanges = true;    
                }
                else
                {
                    Console.WriteLine($"No version found in {s_DirPackageProps} file for {package}! Skipping...");    
                }
            }
            else
            {
                Console.WriteLine($"No condition found in {s_DirPackageProps} file for {condition}! Skipping...");
            }
                
        }

        if (!dryRun && needToWriteChanges)
            File.WriteAllText(project.FullName, xml.ToString());
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
            var package = GetAttributeValue(packageVersion, "Include", false);
            var version = GetAttributeValue(packageVersion, "Version", false);
            var condition = GetAttributeValue(packageVersion.Parent, "Condition", false) ?? string.Empty;

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
    /// <param name="solutionFolder"></param>
    private void WriteDirectoryPackagesConfig(string packageConfigPath, bool dryRun)
    {
        var lines = new List<string>();

        lines.Add("<Project>");
        lines.Add("  <PropertyGroup>");
        lines.Add("    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>");
        lines.Add("  </PropertyGroup>");

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
            File.WriteAllLines(packageConfigPath, lines);
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
    /// <param name="elem"></param>
    /// <param name="name"></param>
    /// <param name="remove"></param>
    /// <returns></returns>
    private string? GetAttributeValue(XElement elem, string name, bool remove)
    {
        var attr = elem.Attributes(name);

        if (attr != null)
        {
            var value = attr.Select(x => x.Value).FirstOrDefault();
            if (remove)
                attr.Remove();
            return value;
        }

        return null;
    }

    /// <summary>
    /// Converts a csproj to Centrally managed packaging.
    /// </summary>
    /// <param name="csprojFile"></param>
    /// <param name="dryRun"></param>
    private void ConvertProject(FileInfo csprojFile, bool dryRun)
    {
        Console.WriteLine($"Processing references for {csprojFile.FullName}...");

        var xml = XDocument.Load(csprojFile.FullName, LoadOptions.PreserveWhitespace);

        var packageReferences = GetDescendants(xml, "PackageReference");

        var needToWriteChanges = false;

        var referencesToRemove = new List<XElement>();
        foreach (var packageReference in packageReferences)
        {
            var removeNodeIfEmpty = false;

            var package = GetAttributeValue(packageReference, "Include", false);

            if (string.IsNullOrEmpty(package))
            {
                package = GetAttributeValue(packageReference, "Update", false);
                removeNodeIfEmpty = true;
            }

            if (string.IsNullOrEmpty(package))
                continue;

            var version = GetAttributeValue(packageReference, "Version", true);

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
            var condition = GetAttributeValue(itemGroup, "Condition", false) ?? string.Empty;

            if (!this.referencesByConditionThenName.TryGetValue(condition, out var referencesForCondition))
            {
                referencesForCondition = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                this.referencesByConditionThenName[condition] = referencesForCondition;
            }

            if (referencesForCondition.TryGetValue(version, out var existing))
            {
                // Existing reference for this package of same or greater version, so skip
                if (version.CompareTo(existing) >= 0)
                    continue;
            }

            Console.WriteLine($" Found new reference: {package} {version} with Condition {condition}");
            referencesForCondition[package] = version;
        }

        foreach (var reference in referencesToRemove)
        {
            reference.Remove();
        }

        if (needToWriteChanges && !dryRun)
        {
            // this keeps the <xml element from appearing on the first line
            File.WriteAllText(csprojFile.FullName, xml.ToString());
        }
    }
}

