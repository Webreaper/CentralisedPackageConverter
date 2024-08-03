using System.Diagnostics.CodeAnalysis;
using NuGet.Versioning;

namespace CentralisedPackageConverter;

public class Versioning
{
    public bool PickMinVersion { get; }

    public bool IgnorePrerelease { get; }

    public VersionComparer Comparer { get; }

    public Versioning(bool pickMinVersion, bool ignorePrerelease, VersionComparison comparison)
    {
        PickMinVersion = pickMinVersion;
        IgnorePrerelease = ignorePrerelease;
        Comparer = new VersionComparer(comparison);
    }

    /// <summary>
    /// Don't consider the version for PackageVersion setting.
    /// True if null or has parts not supported by <see cref="Comparer"/>.
    /// </summary>
    public bool IgnorePackageVersion([NotNullWhen(false)]PackageVersion? version)
    {
        if (version == null)
        {
            return true;
        }

        return IgnorePrerelease && version.NuGetVersion?.IsPrerelease == true;
    }

    /// <summary>
    /// Use the existing and not the next version for the PackageVersion?
    /// Preference of greater/lesser versions according to <see cref="PickMinVersion"/>. 
    /// Consideration of version parts (prerelease, metadata) according to <see cref="Comparer"/>.
    /// </summary>
    /// <param name="existingVersion"></param>
    /// <param name="nextVersion"></param>
    /// <returns></returns>
    public bool PreferExisting(PackageVersion existingVersion, PackageVersion? nextVersion)
    {
        if (existingVersion.IsVariable) return true;
        if (nextVersion == null)
        {
            return true;
        }

        if (nextVersion.IsVariable) return false;

        return PickMinVersion
            ? Comparer.Compare(existingVersion.NuGetVersion, nextVersion.NuGetVersion) <= 0
            : Comparer.Compare(existingVersion.NuGetVersion, nextVersion.NuGetVersion) >= 0;
    }
}