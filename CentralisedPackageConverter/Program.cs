using CentralisedPackageConverter;
using CommandLine;

try
{
    Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(o =>
    {
        var converter = new PackageConverter();

        converter.ProcessConversion(o);

        Output.InfoLine("Processing Complete.");
    });
}
catch (Exception ex)
{
    Output.ErrorLine($"Startup exception: {ex}");
}

