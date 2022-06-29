using System;
using CommandLine;

namespace CentralisedPackageConverter;

public class CommandLineOptions
{
    [Value(0, MetaName = "Root Directory", HelpText = "Root folder to scan for csproj files.", Required = true)]
    public string RootDirectory { get; set; }

    [Option('r', "revert", HelpText = "Revert from Centralised Package Management to csproj-based versions.")]
    public bool Revert { get; set; }

    [Option('d', "dry-run", HelpText = "Read-only mode (make no changes on disk.")]
    public bool DryRun { get; set; }
};
