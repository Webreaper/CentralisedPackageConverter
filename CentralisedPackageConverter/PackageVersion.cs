using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralisedPackageConverter
{
    public class PackageVersion
    {

        public string? VariableName { get; private set; }

        public NuGetVersion? NuGetVersion { get; private set; }


        public static PackageVersion Parse(string value)
        {
            if (!TryParse(value, out var version))
                throw new ArgumentException("Invalid package version", nameof(value));

            return version!;
        }
        public static bool TryParse(string value, out PackageVersion? version)
        {

            if (value != null)
            {
                if (value.Contains("$("))
                {
                    version = new PackageVersion
                    {
                        VariableName = value
                    };
                    return true;
                }

                if (NuGetVersion.TryParse(value, out var nugetVersion))
                {
                    version = new PackageVersion
                    {
                        NuGetVersion = nugetVersion
                    };
                    return true;
                }
            }
            version = null;
            return false;
        }

        public bool IsVariable => !string.IsNullOrWhiteSpace(VariableName);
        public string ToFullString() => NuGetVersion?.ToFullString() ?? VariableName ?? string.Empty;

        public override string ToString() => NuGetVersion?.ToString() ?? VariableName ?? string.Empty;
    }
}
