# CentralisedPackageConverter

Converts a project to use [Centralised Package Management](https://devblogs.microsoft.com/nuget/introducing-central-package-management/)

## What does it do?

To convert a large project to centralised package management, you need to:

* Go through all of the project files and copy the references into the centralised `Directory.Packages.props` file
* Remove all the versions from the package reference entries in the project files.

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

Run the command, passing a folder as the only parameter. The tool will scan for all .NET project files within that 
folder tree, gather up a list of all of the versioned references in the projects, and will then remove the versions
from the project file, and write the entries to the `Directory.Packages.props` file.

## Command-line Options

* Root Directory : Root folder to scan for project files. Required.
* `-r`, `--revert` : Revert from Centralised Package Management to project-file-based versions.
* `-d`, `--dry-run` : Read-only mode (make no changes on disk).
* `-f` : Force changes (don't prompt/check for permission before continuing).
* `-m` : Merge changes with existing directory file.
* `-t`, `--transitive-pinning` : Force versions on transitive dependencies (CentralPackageTransitivePinningEnabled=true).
* `-e`, `--encoding` : Encoding of written files, IANA web name (e.g. `utf-8`, `utf-16`). Default is picked by .NET implementation.
* `-l`, `--linewrap` : Line wrap style: `lf`=Unix, `crlf`=Windows, `cr`=Mac. Default is system style.
* `-v`, `--min-version` : Pick minimum instead of maximum package version number.
* `-p`, `--ignore-prerelease` : Ignore prerelease versions.
* `-c`, `--version-comparison` : Which NuGet version parts to consider: `Default`, `Version`, `VersionRelease`, `VersionReleaseMetadata` (enum `NuGet.Versioning.VersionComparison`).
* `-x`, `--exclude-dirs`, (Default = `^\.|^bin$|^obj$`) Exclude directories matching this regular expression (not search pattern!).

## Comments

Central Package Management CPM does not support floating versions, such as wildcards or range operators. 

Omitted digits will be implicitly added: *"8" == "8.0" == "8.0.0"*. 

### Incomplete or invalid packages file

The *Directory.Packages.props* file may be invalid or incomplete:
* if the versions found in projects are missing, invalid (e.g. floating) or ignored (e.g. prerelease).
* if conditions on *ItemGroup* depend on the project file.

In these cases, manual edits or version selection will be necessary.

## Credits

Thanks to [Thomas Ardal](https://github.com/ThomasArdal) for the suggestion and pointers to get this pushed as a dotnet global command. And thanks to all of the other contributors who have helped make this tool even better.
