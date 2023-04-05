# CentralisedPackageConverter

Converts a project to use [Centralised Package Management](https://devblogs.microsoft.com/nuget/introducing-central-package-management/)

## What does it do?

To convert a large project to centralised package management, you need to:

* Go through all of the csproj files and copy the references into the centralised `Directory.Packages.props` file
* Remove all the versions from the package reference entries in the csproj files.

This can be laborious for large projects, hence this tool. 

# How do I get it?

You can install the tool by running:

```
dotnet tool install CentralisedPackageConverter --global
```

Then just run the tool:

```
central-pkg-converter /Users/markotway/SomeAwesomeProject
```

## How Does it Work?

Run the command, passing a folder as the only parameter. The tool will scan for all `.csproj` files within that 
folder tree, gather up a list of all of the versioned references in the projects, and will then remove the versions
from the `csproj` file, and write the entries to the `Directory.Packages.props` file.

## Command-line Options

* Root Directory : Root folder to scan for csproj files. Required.
* `-r`, `--revert` : Revert from Centralised Package Management to csproj-based versions.
* `-d`, `--dry-run` : Read-only mode (make no changes on disk.
* `-f` : `Force changes (don`t prompt/check for permission before continuing).
* `-m` : `Merge changes with existing directory file.
* `-t`, `--transitive-pinning` : Force versions on transitive dependencies (CentralPackageTransitivePinningEnabled=true).
* `-e`, `--encoding` : Encoding of written files, IANA web name (e.g. `utf-8`, `utf-16`). Default is picked by .NET implementation.
* `-l`, `--linewrap` : Line wrap style: `lf`=Unix, `crlf`=Windows, `cr`=Mac. Default is system style.
* `-v`, `--min-version` : Pick minimum instead of maximum package version number.
* `-p`, `--ignore-prerelease` : Ignore prerelease versions.
* `-c`, `--version-comparison` : Which NuGet version parts to consider: `Default`, `Version`, `VersionRelease`, `VersionReleaseMetadata` (enum `NuGet.Versioning.VersionComparison`).

## Credits

Thanks to [Thomas Ardal](https://github.com/ThomasArdal) for the suggestion and pointers to get this pushed as a dotnet global command. 
