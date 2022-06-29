using CentralisedPackageConverter;

var folder = args.FirstOrDefault();

if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
{

    var converter = new PackageConverter();

    converter.ProcessConversion(folder);

    Console.WriteLine("Complete.");
}
else
    Console.WriteLine("Usage: CentralisedPackageConverter foldername");

