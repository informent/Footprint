using System.IO;

namespace Footprint;

public sealed record SpaceEntry(string Path, long Bytes, bool IsDirectory);
public sealed record ScanIssue(string Path, string Reason);
public sealed record ScanReport(IReadOnlyList<SpaceEntry> Entries, IReadOnlyList<ScanIssue> Issues, int FilesScanned, long TotalBytes);

public static class ScanEngine
{
    public static IReadOnlyList<SpaceEntry> Scan(string root, CancellationToken cancellationToken = default, IProgress<SpaceEntry>? progress = null)
        => ScanDetailed(root, cancellationToken, progress).Entries;

    public static ScanReport ScanDetailed(string root, CancellationToken cancellationToken = default, IProgress<SpaceEntry>? progress = null)
    {
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var rootPath = Normalize(root);
        var files = new List<(string Path, long Bytes)>();
        var issues = new List<ScanIssue>();
        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            IEnumerable<string> children;
            try
            {
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                {
                    issues.Add(new ScanIssue(directory, "Skipped reparse point"));
                    continue;
                }
                children = Directory.EnumerateFileSystemEntries(directory).ToArray();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { issues.Add(new ScanIssue(directory, ex.Message)); continue; }

            foreach (var path in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReparsePoint) != 0) { issues.Add(new ScanIssue(path, "Skipped reparse point")); continue; }
                    if ((attributes & FileAttributes.Directory) != 0) { pending.Push(path); continue; }
                    var bytes = new FileInfo(path).Length;
                    files.Add((Path.GetFullPath(path), bytes));
                    progress?.Report(new SpaceEntry(path, bytes, false));
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or FileNotFoundException) { issues.Add(new ScanIssue(path, ex.Message)); }
            }
        }

        var result = files.Select(x => new SpaceEntry(x.Path, x.Bytes, false)).ToList();
        var directories = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var current = Path.GetDirectoryName(file.Path);
            while (current is not null && IsWithinOrEqual(current, rootPath))
            {
                directories[current] = directories.GetValueOrDefault(current) + file.Bytes;
                if (current.Equals(rootPath, StringComparison.OrdinalIgnoreCase)) break;
                current = Path.GetDirectoryName(current);
            }
        }
        result.AddRange(directories.Select(x => new SpaceEntry(x.Key, x.Value, true)));
        return new ScanReport(result.OrderByDescending(x => x.Bytes).ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToArray(), issues, files.Count, files.Sum(x => x.Bytes));
    }

    private static string Normalize(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    private static bool IsWithinOrEqual(string candidate, string root)
    {
        var normalized = Normalize(candidate);
        return normalized.Equals(root, StringComparison.OrdinalIgnoreCase) || normalized.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
