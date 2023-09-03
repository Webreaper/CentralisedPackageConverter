using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CentralisedPackageConverter;

public class DirectoryCrawler
{
    public Regex ExcludesRegex { get; }

    public Regex FilesRegex { get; }


    public DirectoryCrawler(string excludesRegexString, Regex filesRegex)
    : this(new Regex(excludesRegexString), filesRegex)
    {

    }
    public DirectoryCrawler(Regex excludesRegex, Regex filesRegex)
    {
        ExcludesRegex = excludesRegex;
        FilesRegex = filesRegex;
    }

    /// <summary>
    /// Enumerate all directories not filtered out by <see cref="ExcludesRegex"/>,
    /// including <paramref name="startDirectory"/>.
    /// </summary>
    public IEnumerable<DirectoryInfo> EnumerateRelevantDirectories(DirectoryInfo startDirectory)
    {
        yield return startDirectory;

        foreach (var relevantDir in startDirectory.EnumerateDirectories()
                     .Where(d => !ExcludesRegex.IsMatch(d.Name)))
        {
            foreach (var subdir in EnumerateRelevantDirectories(relevantDir))
            {
                yield return subdir;
            }
        }
    }

    public IEnumerable<FileInfo> EnumerateRelevantFiles(DirectoryInfo directory, SearchOption searchOption)
    {
        return searchOption == SearchOption.TopDirectoryOnly
            ? directory.EnumerateFiles().Where(f => FilesRegex.IsMatch(f.Name))
            : EnumerateRelevantDirectories(directory).SelectMany(d => EnumerateRelevantFiles(d, SearchOption.TopDirectoryOnly));
    }
}