using CentralisedPackageConverter;
using CommandLine;

try
{
    Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(o =>
    {
        var converter = new PackageConverter();

        converter.ProcessConversion(o.RootDirectory, o.Revert, o.DryRun, o.Force, o.Merge);

        Console.WriteLine("Processing Complete.");
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Startup exception: {ex}");
}

