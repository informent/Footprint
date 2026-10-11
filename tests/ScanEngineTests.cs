using Footprint;
using System.Diagnostics;
var root = Path.Combine(Path.GetTempPath(), "openspace-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(Path.Combine(root, "nested")); File.WriteAllText(Path.Combine(root, "small.txt"), "small"); File.WriteAllBytes(Path.Combine(root, "nested", "large.bin"), new byte[4096]);
var progressItems = new List<SpaceEntry>(); var report = ScanEngine.ScanDetailed(root, progress: new InlineProgress(progressItems)); var results = report.Entries;
if (!results.Any(x => x.Path.EndsWith("large.bin") && x.Bytes == 4096) || results.First().Bytes < 4096) throw new Exception("Scan did not rank file sizes correctly.");
if (report.FilesScanned != 2 || report.TotalBytes != 4101 || progressItems.Count != 2) throw new Exception("Scan summary or progress is incorrect.");
var rootEntry = results.Single(x => x.IsDirectory && x.Path.Equals(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)); if (rootEntry.Bytes != 4101) throw new Exception("Root aggregation is incorrect.");
using var cts = new CancellationTokenSource(); cts.Cancel(); try { ScanEngine.Scan(root, cts.Token); throw new Exception("Cancellation was ignored."); } catch (OperationCanceledException) { }
Console.WriteLine("PASS: fault-tolerant read-only scan, exact aggregation, progress, and cancellation");

var outside = root + "-outside";
var linkedRoot = Path.Combine(root, "linked-root");
Directory.CreateDirectory(outside);
File.WriteAllText(Path.Combine(outside, "outside-only.txt"), "must not be scanned");
try
{
    var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var argument in new[] { "/c", "mklink", "/J", linkedRoot, outside }) start.ArgumentList.Add(argument);
    using var junction = Process.Start(start) ?? throw new Exception("Could not start junction fixture command.");
    junction.WaitForExit();
    if (junction.ExitCode != 0) throw new Exception("Could not create linked-root fixture.");
    var linkedReport = ScanEngine.ScanDetailed(linkedRoot);
    if (linkedReport.FilesScanned != 0 || linkedReport.Entries.Count != 0 || !linkedReport.Issues.Any(issue => issue.Reason == "Skipped reparse point"))
        throw new Exception("A linked scan root was traversed instead of reported and skipped.");
    Console.WriteLine("PASS: linked scan root is reported and skipped");
}
finally
{
    if (Directory.Exists(linkedRoot)) Directory.Delete(linkedRoot);
    if (Directory.Exists(outside)) Directory.Delete(outside, true);
    Directory.Delete(root, true);
}

sealed class InlineProgress(List<SpaceEntry> items) : IProgress<SpaceEntry> { public void Report(SpaceEntry value) => items.Add(value); }
