# CentralisedPackageConverter

Converts a project to use [Centralised Package Management](https://devblogs.microsoft.com/nuget/introducing-central-package-management/)

## What does it do?

To convert a large project to centralised package management, you need to:

* Go through all of the csproj files and copy the references into the centralised `Directory.Packages.props` file
* Remove all the versions from the package reference entries in the `.csproj`, `.vbproj`, `.fsproj` project files.

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

Run the command, passing a folder as the only parameter. The tool will scan for all project files within that 
folder tree, gather up a list of all of the versioned references in the projects, and will then remove the versions
from the project file, and write the entries to the `Directory.Packages.props` file.

## Command-line Options

* `-d` will force the tool to run in 'dry run' mode, meaning no changes will be written to disk.
* `-r` will reverse the conversion process - writing the versions back to the csproj files, and deleting the `Directory.Package.props` file.
* `-y` will skip the "Are you sure you want to do this" prompt and make the changes regardless. Be careful!

## Credits

Thanks to [Thomas Ardal](https://github.com/ThomasArdal) for the suggestion and pointers to get this pushed as a dotnet global command. 
