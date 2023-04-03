using CentralisedPackageConverter;
using CommandLine;

try
{
    Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(o =>
    {
        var converter = new PackageConverter();

        converter.ProcessConversion(o);

        Console.WriteLine("Processing Complete.");
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Startup exception: {ex}");
}

