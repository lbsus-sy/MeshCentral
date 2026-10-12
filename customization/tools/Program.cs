using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Forms;

namespace LakersDesktopTools;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ToolsForm());
    }
}

internal sealed class ToolsForm : Form
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SystemParametersInfo(uint action, uint param, string value, uint flags);

    private const uint SpiSetDesktopWallpaper = 0x0014;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendChange = 0x02;
    private readonly Label _status = new() { AutoSize = true, Text = "Tools run locally. Nothing is uploaded." };

    public ToolsForm()
    {
        Text = "Lakers MeshCentral Desktop Tools";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(680, 330);
        MinimumSize = new Size(620, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var title = new Label
        {
            Text = "Local desktop tools",
            Font = new Font(Font.FontFamily, 15, FontStyle.Bold),
            AutoSize = true
        };
        var notice = new Label
        {
            Text = "Wallpaper changes apply only to the current signed-in user's desktop. Diagnostics are read-only and saved locally; inspect the report before sharing it because it may contain usernames, file paths, service names, and event messages.",
            AutoSize = true,
            MaximumSize = new Size(600, 0)
        };
        var wallpaper = new Button { Text = "Choose wallpaper…", AutoSize = true, MinimumSize = new Size(170, 38) };
        var diagnostics = new Button { Text = "Collect diagnostics…", AutoSize = true, MinimumSize = new Size(190, 38) };
        wallpaper.Click += (_, _) => SetWallpaper();
        diagnostics.Click += async (_, _) => await CollectDiagnosticsAsync();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 8, 0, 8) };
        buttons.Controls.Add(wallpaper);
        buttons.Controls.Add(diagnostics);

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(18), AutoScroll = true };
        layout.Controls.Add(title);
        layout.Controls.Add(notice);
        layout.Controls.Add(buttons);
        layout.Controls.Add(_status);
        Controls.Add(layout);
    }

    private void SetWallpaper()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose an image for this user's desktop",
            Filter = "Wallpaper images|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var path = Path.GetFullPath(dialog.FileName);
        var answer = MessageBox.Show(this,
            $"Set this image as the desktop wallpaper for the current user?\n\n{path}",
            "Confirm wallpaper change", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true)
                ?? Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
            {
                if (key is null) throw new InvalidOperationException("Unable to open the current user's desktop settings.");
                key.SetValue("Wallpaper", path, RegistryValueKind.String);
                key.SetValue("WallpaperStyle", "10", RegistryValueKind.String);
                key.SetValue("TileWallpaper", "0", RegistryValueKind.String);
            }
            if (!SystemParametersInfo(SpiSetDesktopWallpaper, 0, path, SpifUpdateIniFile | SpifSendChange))
                throw new InvalidOperationException($"Windows rejected the wallpaper change (Win32 error {Marshal.GetLastWin32Error()}).");
            _status.Text = $"Wallpaper updated for {Environment.UserName}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Wallpaper change failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task CollectDiagnosticsAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save local diagnostics report",
            Filter = "JSON report (*.json)|*.json",
            FileName = $"lakers-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.json",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _status.Text = "Collecting local read-only diagnostics…";
        try
        {
            var processes = Process.GetProcesses().Select(p =>
            {
                string? executable = null;
                try { executable = p.MainModule?.FileName; } catch { }
                double? memoryMb = null;
                try { memoryMb = Math.Round(p.WorkingSet64 / 1024d / 1024d, 1); } catch { }
                return new { Name = Safe(() => p.ProcessName), Id = p.Id, ExecutablePath = executable, WorkingSetMB = memoryMb };
            }).OrderBy(p => p.Name).ToArray();

            var services = await RunCommandAsync("sc.exe", new[] { "query", "state=", "all" }, TimeSpan.FromSeconds(20));
            var systemEvents = await RunCommandAsync("wevtutil.exe",
                new[] { "qe", "System", "/q:*[System[(Level=1 or Level=2 or Level=3)]]", "/c:100", "/rd:true", "/f:text" },
                TimeSpan.FromSeconds(20));
            var applicationEvents = await RunCommandAsync("wevtutil.exe",
                new[] { "qe", "Application", "/q:*[System[(Level=1 or Level=2 or Level=3)]]", "/c:100", "/rd:true", "/f:text" },
                TimeSpan.FromSeconds(20));

            var report = new
            {
                SchemaVersion = 1,
                CollectedAt = DateTimeOffset.Now,
                ComputerName = Environment.MachineName,
                UserName = Environment.UserName,
                Processes = processes,
                Services = services,
                RecentSystemEvents = systemEvents,
                RecentApplicationEvents = applicationEvents,
                PrivacyNotice = "Local read-only collection. Nothing is uploaded. Review for sensitive usernames, paths, hostnames, and event messages before sharing."
            };
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(dialog.FileName, json, new UTF8Encoding(false));
            _status.Text = $"Report saved locally: {dialog.FileName}";
            MessageBox.Show(this, $"Diagnostics saved. Review the JSON for sensitive data before sharing.\n\n{dialog.FileName}",
                "Diagnostics complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _status.Text = "Diagnostics collection failed.";
            MessageBox.Show(this, ex.Message, "Diagnostics error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string? Safe(Func<string> get)
    {
        try { return get(); } catch { return null; }
    }

    private static async Task<object> RunCommandAsync(string fileName, string[] arguments, TimeSpan timeout)
    {
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            using var process = new Process { StartInfo = start };
            if (!process.Start()) return new { ExitCode = -1, Output = "", Error = "Process could not start." };
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            using var cts = new CancellationTokenSource(timeout);
            try { await process.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return new { ExitCode = -2, Output = "", Error = "Command timed out." };
            }
            var output = await outputTask;
            var error = await errorTask;
            return new { ExitCode = process.ExitCode, Output = Limit(output), Error = Limit(error) };
        }
        catch (Exception ex) { return new { ExitCode = -1, Output = "", Error = ex.Message }; }
    }

    private static string Limit(string value) => value.Length > 100_000 ? value[..100_000] + "\n[truncated]" : value;
}
