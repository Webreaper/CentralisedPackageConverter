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
    public bool IgnorePackageVersion([NotNullWhen(false)]NuGetVersion? version)
    {
        if (version == null)
        {
            return true;
        }

        return IgnorePrerelease && version.IsPrerelease;
    }

    /// <summary>
    /// Use the existing and not the next version for the PackageVersion?
    /// Preference of greater/lesser versions according to <see cref="PickMinVersion"/>. 
    /// Consideration of version parts (prerelease, metadata) according to <see cref="Comparer"/>.
    /// </summary>
    /// <param name="existingVersion"></param>
    /// <param name="nextVersion"></param>
    /// <returns></returns>
    public bool PreferExisting(NuGetVersion existingVersion, NuGetVersion? nextVersion)
    {
        if (nextVersion == null)
        {
            return true;
        }

        return PickMinVersion
            ? Comparer.Compare(existingVersion, nextVersion) <= 0
            : Comparer.Compare(existingVersion, nextVersion) >= 0;
    }
}