using NuGet.Versioning;
using System.Diagnostics.CodeAnalysis;


namespace CentralisedPackageConverter
{
    public class PackageVersion
    {

        public string? VariableName { get; private set; }

        public NuGetVersion? NuGetVersion { get; private set; }

        [MemberNotNullWhen(true, nameof(NuGetVersion))]
        public bool IsFixedVersion { get; private init; }

        public static PackageVersion Parse(string value)
        {
            if (!TryParse(value, out var version))
                throw new ArgumentException("Invalid package version", nameof(value));

            return version!;
        }
        public static bool TryParse(string? value, out PackageVersion? version)
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

                var isFixed = false;

                // Use a span to avoid additional string allocations and trim white space
                var span = value.AsSpan().Trim();
                
                // Check if the value is surrounded by square brackets -> this indicates a fixed version
                if (span.Length > 2 && span[0] == '[' && span[^1] == ']')
                {
                    value = span.Slice(1, span.Length - 2).ToString();
                    isFixed = true;
                }

                if (NuGetVersion.TryParse(value, out var nugetVersion))
                {
                    version = new PackageVersion
                    {
                        NuGetVersion = nugetVersion,
                        IsFixedVersion = isFixed
                    };
                    return true;
                }
            }
            version = null;
            return false;
        }

        public bool IsVariable => !string.IsNullOrWhiteSpace(VariableName);
        public string ToFullString() =>
            IsFixedVersion ?
                $"[{NuGetVersion.ToFullString()}]" :
                NuGetVersion?.ToFullString() ?? VariableName ?? string.Empty;

        public override string ToString() => ToFullString();
    }
}
