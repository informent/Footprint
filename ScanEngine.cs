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
        var directories = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
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
                directories.TryAdd(directory, 0);
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
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
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

    private static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    private static bool IsWithinOrEqual(string candidate, string root)
    {
        var normalized = Normalize(candidate);
        return normalized.Equals(root, StringComparison.OrdinalIgnoreCase) || normalized.StartsWith(Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}

public static class ScanExport
{
    public static void WriteCsv(ScanReport report, string path)
    {
        ArgumentNullException.ThrowIfNull(report);
        // A new report file cannot overwrite scanned data or a previous report.
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(true));
        writer.WriteLine("Kind,Path,Bytes,Detail");
        writer.WriteLine($"Summary,,{report.TotalBytes},{Cell($"{report.FilesScanned} files; {report.Entries.Count(x => x.IsDirectory)} folders; {report.Issues.Count} skipped items")}");
        foreach (var entry in report.Entries) writer.WriteLine($"{(entry.IsDirectory ? "Folder" : "File")},{Cell(entry.Path)},{entry.Bytes},");
        foreach (var issue in report.Issues) writer.WriteLine($"Skipped,{Cell(issue.Path)},,{Cell(issue.Reason)}");
    }
    private static string Cell(string text)
    {
        // Treat arbitrary filenames and error messages as spreadsheet text.
        if (text.TrimStart().StartsWith('=') || text.TrimStart().StartsWith('+') || text.TrimStart().StartsWith('-') || text.TrimStart().StartsWith('@') || text.StartsWith('\t') || text.StartsWith('\r')) text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
