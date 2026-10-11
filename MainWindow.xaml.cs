using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;
using TextBlock = System.Windows.Controls.TextBlock;

namespace Footprint;

public partial class MainWindow : Window
{
    private string? root;
    private CancellationTokenSource? scanCts;
    private readonly System.Windows.Controls.ProgressBar largestBar = new() { Height = 10, Minimum = 0, Maximum = 100, Margin = new Thickness(0, 8, 0, 12) };
    private readonly TextBlock largestText = new() { Foreground = System.Windows.Media.Brushes.Gray };
    private readonly TextBlock issuesText = new() { Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0, 0, 0, 12) };

    public MainWindow()
    {
        InitializeComponent();
        if (TotalText.Parent is System.Windows.Controls.Panel panel)
        {
            largestText.Text = "Largest location";
            issuesText.Text = "Skipped items will be reported here";
            panel.Children.Insert(3, largestText);
            panel.Children.Insert(4, largestBar);
            panel.Children.Insert(5, issuesText);
        }
    }

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog { Description = "Choose a folder to analyze" };
        if (dialog.ShowDialog() == Forms.DialogResult.OK) { root = dialog.SelectedPath; PathText.Text = root; StatusText.Text = "Ready to scan"; }
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        if (root is null) return;
        scanCts?.Cancel(); scanCts = new CancellationTokenSource(); ScanButton.IsEnabled = false; CancelButton.IsEnabled = true; StatusText.Text = "Scanning…"; issuesText.Text = "Collecting access evidence…";
        try
        {
            var progress = new Progress<SpaceEntry>(entry => StatusText.Text = $"Scanning {Path.GetFileName(entry.Path)}");
            var report = await Task.Run(() => ScanEngine.ScanDetailed(root, scanCts.Token, progress), scanCts.Token);
            var entries = report.Entries;
            EntryList.ItemsSource = entries.Take(100).Select(x => $"{(x.IsDirectory ? "▣" : "•")}  {Path.GetFileName(x.Path)}    {Format(x.Bytes)}").ToArray();
            var largest = entries.FirstOrDefault(); TotalText.Text = Format(report.TotalBytes);
            largestText.Text = largest is null ? "Largest location" : $"Largest: {Path.GetFileName(largest.Path)}  ·  {Format(largest.Bytes)}";
            largestBar.Value = report.TotalBytes == 0 || largest is null ? 0 : Math.Min(100, largest.Bytes * 100d / report.TotalBytes);
            CountText.Text = $"{report.FilesScanned:N0} files · {entries.Count(x => x.IsDirectory):N0} folders";
            if (report.Issues.Count == 0)
            {
                issuesText.Text = "Complete coverage · no inaccessible entries";
                issuesText.Foreground = System.Windows.Media.Brushes.SeaGreen;
                StatusText.Text = "Scan complete";
            }
            else
            {
                issuesText.Text = $"{report.Issues.Count:N0} item(s) skipped · inaccessible or reparse-point paths";
                issuesText.ToolTip = string.Join(Environment.NewLine, report.Issues.Take(20).Select(x => $"{x.Path}: {x.Reason}"));
                issuesText.Foreground = System.Windows.Media.Brushes.DarkOrange;
                StatusText.Text = $"Scan complete with {report.Issues.Count:N0} warning(s)";
            }
        }
        catch (OperationCanceledException) { StatusText.Text = "Scan cancelled"; issuesText.Text = "Results may be incomplete"; }
        catch (Exception ex) { StatusText.Text = ex.Message; issuesText.Text = "Scan failed before coverage could be measured"; }
        finally { ScanButton.IsEnabled = true; CancelButton.IsEnabled = false; }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => scanCts?.Cancel();
    private static string Format(long bytes) => bytes switch { >= 1_000_000_000 => $"{bytes / 1_000_000_000d:0.0} GB", >= 1_000_000 => $"{bytes / 1_000_000d:0.0} MB", >= 1_000 => $"{bytes / 1_000d:0.0} KB", _ => $"{bytes:N0} B" };
}
