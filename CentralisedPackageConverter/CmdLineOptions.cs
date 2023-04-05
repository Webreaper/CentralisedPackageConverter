using CommandLine;
using NuGet.Versioning;

namespace CentralisedPackageConverter;

public class CommandLineOptions
{
    [Value(0, MetaName = "Root Directory", HelpText = "Root folder to scan for csproj files.", Required = true)]
    public string RootDirectory { get; set; } = string.Empty;

    [Option('r', "revert", HelpText = "Revert from Centralised Package Management to csproj-based versions.")]
    public bool Revert { get; set; }

    [Option('d', "dry-run", HelpText = "Read-only mode (make no changes on disk.")]
    public bool DryRun { get; set; }

    [Option('f', HelpText = "Force changes (don't prompt/check for permission before continuing).")]
    public bool Force { get; set; }

    [Option('m', HelpText = "Merge changes with existing directory file")]
    public bool Merge { get; set; }

    [Option('t', "transitive-pinning", HelpText = "Force versions on transitive dependencies (CentralPackageTransitivePinningEnabled=true)")]
    public bool TransitivePinning { get; set; }

    [Option('e', "encoding", HelpText = "Encoding of written files, IANA web name (e.g. 'utf-8', 'utf-16'). Default is picked by .NET implementation.")]
    public string? EncodingWebName { get; set; }

    [Option('l', "linewrap", HelpText = "Line wrap style: 'lf'=Unix, 'crlf'=Windows, 'cr'=Mac. Default is system style." )]
    public string? LineWrap { get; set; }

    [Option('v', "min-version", HelpText = "Pick minimum instead of maximum package version number.")]
    public bool PickMinVersion { get; set; }

    [Option('p', "ignore-prerelease", HelpText = "Ignore prerelease versions.")]
    public bool IgnorePrerelease { get; set; }

    [Option('c', "version-comparison", 
        HelpText = $"Which NuGet version parts to consider (enum {nameof(VersionComparison)}): " +
            $"{nameof(VersionComparison.Default)}, {nameof(VersionComparison.Version)}, " + 
            $"{nameof(VersionComparison.VersionRelease)}, {nameof(VersionComparison.VersionReleaseMetadata)}")]
    public VersionComparison VersionComparison { get; set; }
};
