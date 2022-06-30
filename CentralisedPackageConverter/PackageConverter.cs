using System;
using System.IO;
using System.Xml.Linq;

namespace CentralisedPackageConverter;

public class PackageConverter
{
    private IDictionary<string, string> allReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private const string s_DirPackageProps = "Directory.Packages.props";

    private static readonly HashSet<string> s_extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

        // If we're reverting, read the references from the central file.
        if (revert)
            ReadDirectoryPackagePropsFile(packageConfigPath);

        if (revert)
        {
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

            if (allReferences.Any())
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

        var refs = xml.Descendants("PackageReference");

        bool needToWriteChanges = false;

        foreach (var reference in refs)
        {
            var package = GetAttributeValue(reference, "Include", false);

            if (allReferences.TryGetValue(package, out var version))
            {
                reference.SetAttributeValue("Version", version);
                needToWriteChanges = true;
            }
            else
                Console.WriteLine($"No version found in {s_DirPackageProps} file for {package}! Skipping...");
        }

        if (!dryRun && needToWriteChanges)
            xml.Save(project.FullName);
    }

    /// <summary>
    /// Read the list of references and versions from the Directory.Package.props file.
    /// </summary>
    /// <param name="packageConfigPath"></param>
    private void ReadDirectoryPackagePropsFile(string packageConfigPath)
    {
        var xml = XDocument.Load(packageConfigPath);

        var refs = xml.Descendants("PackageVersion");

        foreach (var reference in refs)
        {
            var package = GetAttributeValue(reference, "Include", false);
            var version = GetAttributeValue(reference, "Version", false);

            allReferences[package] = version;
        }

        Console.WriteLine($"Read {allReferences.Count} references from {packageConfigPath}");
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
        lines.Add("  <ItemGroup>");

        foreach (var kvp in allReferences.OrderBy(x => x.Key))
        {
            lines.Add($"    <PackageVersion Include=\"{kvp.Key}\" Version=\"{kvp.Value}\" />");
        }

        lines.Add("  </ItemGroup>");
        lines.Add("</Project>");

        Console.WriteLine($"Writing {allReferences.Count} refs to {s_DirPackageProps} to {packageConfigPath}...");

        if (dryRun)
            lines.ForEach(x => Console.WriteLine(x));
        else
            File.WriteAllLines(packageConfigPath, lines);
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

        var refs = xml.Descendants("PackageReference");

        bool needToWriteChanges = false;

        var referencesToRemove = new List<XElement>();
        foreach (var reference in refs)
        {
            var removeNodeIfEmpty = false;

            var package = GetAttributeValue(reference, "Include", false);

            if (string.IsNullOrEmpty(package))
            {
                package = GetAttributeValue(reference, "Update", false);
                removeNodeIfEmpty = true;
            }

            if (string.IsNullOrEmpty(package))
                continue;

            var version = GetAttributeValue(reference, "Version", true);

            if (!string.IsNullOrEmpty(version))
            {
                // If there is only an Update attribute left, and no child elements, then this node
                // isn't useful any more, so we can remove it entirely
                if (removeNodeIfEmpty && reference.Attributes().Count() == 1 && !reference.Elements().Any())
                    referencesToRemove.Add(reference);

                needToWriteChanges = true;

                if (allReferences.TryGetValue(package, out var existingVer))
                {
                    // Existing reference for this package of same or greater version, so skip
                    if (version.CompareTo(existingVer) >= 0)
                        continue;
                }

                Console.WriteLine($" Found new reference: {package} {version}");
                allReferences[package] = version;
            }
        }

        foreach (var reference in referencesToRemove)
        {
            reference.Remove();
        }

        if (needToWriteChanges && !dryRun)
            xml.Save(csprojFile.FullName);
    }
}

