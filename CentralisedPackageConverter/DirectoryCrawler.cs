using System.IO.Enumeration;
using System.Text.RegularExpressions;

namespace CentralisedPackageConverter;

public class DirectoryCrawler
{
    public Regex ExcludesRegex { get; }
    public HashSet<string> AllowedFileExtensions { get; }

    public DirectoryCrawler(string excludesRegexString, HashSet<string> allowedFileExtensions)
    : this(new Regex(excludesRegexString), allowedFileExtensions)
    {

    }
    public DirectoryCrawler(Regex excludesRegex, HashSet<string> allowedFileExtensions)
    {
        ExcludesRegex = excludesRegex;
        AllowedFileExtensions = allowedFileExtensions;
    }


    public IEnumerable<FileInfo> EnumerateRelevantFiles(DirectoryInfo directory, SearchOption searchOption)
    {
        return new FileSystemEnumerable<FileInfo>(
            directory: directory.FullName,
            transform: (ref FileSystemEntry entry) => new FileInfo(entry.ToFullPath()),
            options: new EnumerationOptions { RecurseSubdirectories = searchOption == SearchOption.AllDirectories }
        )
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
            {
                return !entry.IsDirectory && AllowedFileExtensions.Contains(Path.GetExtension(entry.FileName).ToString());
            },
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
            {
                return !ExcludesRegex.IsMatch(entry.FileName);
            },
        };
    }
}