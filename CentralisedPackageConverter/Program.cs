using CentralisedPackageConverter;
using CommandLine;

try
{
    Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(o =>
    {
        var converter = new PackageConverter();

        converter.ProcessConversion(o.RootDirectory, o.Revert, o.DryRun);

        Console.WriteLine("Complete.");
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Startup exception: {ex}");
}

