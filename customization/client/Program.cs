using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace LakersMeshAgentInstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm());
    }
}

internal sealed class InstallerForm : Form
{
    private readonly TextBox _agentPath = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly CheckBox _consent = new()
    {
        AutoSize = true,
        Text = "I own or am authorized to manage this device, and I trust the selected agent's MeshCentral server.",
        MaximumSize = new Size(610, 0)
    };
    private readonly Button _install = new() { Text = "Install agent", AutoSize = true, Enabled = false };
    private readonly Button _uninstall = new() { Text = "Uninstall agent…", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, Text = "No agent selected." };
    private readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "LakersMeshCentral", "installer.log");

    public InstallerForm()
    {
        Text = "Lakers MeshCentral Agent Installer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 320);
        Size = new Size(760, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var heading = new Label
        {
            Text = "Install an authorized MeshCentral agent",
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        var warning = new Label
        {
            Text = "This installer requires administrator approval (UAC). Select the Windows agent EXE downloaded from a MeshCentral server you trust. Installation registers a persistent Windows service for remote management. Do not install an agent from an unknown source.",
            AutoSize = true,
            MaximumSize = new Size(680, 0)
        };
        var browse = new Button { Text = "Choose agent EXE…", AutoSize = true };
        browse.Click += (_, _) => ChooseAgent();
        _consent.CheckedChanged += (_, _) => RefreshInstallState();
        _install.Click += async (_, _) => await InstallAgentAsync();
        _uninstall.Click += async (_, _) => await UninstallAgentAsync();

        var fileRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
        fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fileRow.Controls.Add(_agentPath, 0, 0);
        fileRow.Controls.Add(browse, 1, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(_install);
        buttons.Controls.Add(_uninstall);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 6,
            AutoSize = false
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(warning, 0, 1);
        layout.Controls.Add(fileRow, 0, 2);
        layout.Controls.Add(_consent, 0, 3);
        layout.Controls.Add(_status, 0, 4);
        layout.Controls.Add(buttons, 0, 5);
        Controls.Add(layout);
    }

    private void ChooseAgent()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select the MeshCentral Windows agent downloaded from your trusted server",
            Filter = "MeshCentral agent executable (MeshAgent*.exe)|MeshAgent*.exe|Windows executables (*.exe)|*.exe",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var info = new FileInfo(dialog.FileName);
            if (info.Length < 4096 || info.Length > 250L * 1024 * 1024)
                throw new InvalidDataException("The selected file size is outside the expected range.");
            using var stream = File.OpenRead(info.FullName);
            if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z')
                throw new InvalidDataException("The selected file is not a Windows PE executable.");
            _agentPath.Text = info.FullName;
            _status.Text = $"Selected: {info.Name} ({info.Length / 1024:N0} KB). Review its source before installing.";
            RefreshInstallState();
        }
        catch (Exception ex)
        {
            _agentPath.Clear();
            _status.Text = "Invalid file.";
            MessageBox.Show(this, ex.Message, "Agent file validation failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshInstallState();
        }
    }

    private void RefreshInstallState() => _install.Enabled = _consent.Checked && File.Exists(_agentPath.Text);


    private async Task UninstallAgentAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select the installed MeshCentral agent executable",
            Filter = "MeshCentral agent executable (MeshAgent*.exe)|MeshAgent*.exe|Windows executables (*.exe)|*.exe",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!string.Equals(Path.GetFileName(dialog.FileName).StartsWith("MeshAgent", StringComparison.OrdinalIgnoreCase) ? "MeshAgent" : "",
                "MeshAgent", StringComparison.Ordinal))
        {
            MessageBox.Show(this, "Select the installed MeshAgent executable.", "Agent required",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var answer = MessageBox.Show(this,
            $"Remove the selected MeshCentral agent service?\n\nFile: {dialog.FileName}\n\nThis will disconnect the device from its management server.",
            "Confirm agent removal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        _install.Enabled = false;
        _uninstall.Enabled = false;
        _status.Text = "Removing agent service…";
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "LakersMeshCentral", "installer.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            var hash = await Task.Run(() => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dialog.FileName))));
            await File.AppendAllTextAsync(logPath,
                $"\n[{DateTimeOffset.Now:O}] User={Environment.UserName}; Source={dialog.FileName}; SHA256={hash}; action=fulluninstall\n",
                Encoding.UTF8);
            var start = new ProcessStartInfo
            {
                FileName = dialog.FileName,
                Arguments = "-fulluninstall",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(dialog.FileName)!
            };
            using var process = new Process { StartInfo = start };
            if (!process.Start()) throw new InvalidOperationException("Windows could not start the selected agent.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException("The uninstaller exceeded three minutes. Check the local log and service state.");
            }
            await File.AppendAllTextAsync(logPath,
                $"exitCode={process.ExitCode}\nstdout:\n{await stdoutTask}\nstderr:\n{await stderrTask}\n", Encoding.UTF8);
            if (process.ExitCode == 0)
            {
                _status.Text = "Uninstall command completed. Verify the service is removed.";
                MessageBox.Show(this, $"Uninstall command completed. Verify the device disconnects from the server.\n\nLog: {logPath}",
                    "Uninstall complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _status.Text = $"Uninstaller returned exit code {process.ExitCode}.";
                MessageBox.Show(this, $"Agent returned exit code {process.ExitCode}. See the local log.\n\n{logPath}",
                    "Uninstall failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _status.Text = "Uninstall did not complete.";
            try { await File.AppendAllTextAsync(logPath, $"[{DateTimeOffset.Now:O}] ERROR: {ex}\n", Encoding.UTF8); } catch { }
            MessageBox.Show(this, ex.Message, "Uninstall error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _install.Enabled = _consent.Checked && File.Exists(_agentPath.Text); _uninstall.Enabled = true; }
    }

    private async Task InstallAgentAsync()
    {
        if (!_consent.Checked || !File.Exists(_agentPath.Text)) return;
        var answer = MessageBox.Show(this,
            $"Install the selected agent as a Windows service?\n\nFile: {_agentPath.Text}\n\nOnly continue if you trust the MeshCentral server that generated this file.",
            "Confirm remote-management agent installation",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        _install.Enabled = false;
        _status.Text = "Installing agent service…";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            var hash = await Task.Run(() => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_agentPath.Text))));
            await File.AppendAllTextAsync(_logPath,
                $"\n[{DateTimeOffset.Now:O}] User={Environment.UserName}; Source={_agentPath.Text}; SHA256={hash}; action=fullinstall\n",
                Encoding.UTF8);

            var start = new ProcessStartInfo
            {
                FileName = _agentPath.Text,
                Arguments = "-fullinstall",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(_agentPath.Text)!
            };
            using var process = new Process { StartInfo = start, EnableRaisingEvents = true };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };
            if (!process.Start()) throw new InvalidOperationException("Windows could not start the selected agent.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException("The agent installer exceeded three minutes. Check the log and server before retrying.");
            }
            await File.AppendAllTextAsync(_logPath,
                $"exitCode={process.ExitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}\n", Encoding.UTF8);

            if (process.ExitCode == 0)
            {
                _status.Text = "Install command completed. Verify the device appears in your MeshCentral server.";
                MessageBox.Show(this, $"Installation command completed. Confirm the device is online in your server.\n\nLog: {_logPath}",
                    "Installation complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _status.Text = $"Installer returned exit code {process.ExitCode}.";
                MessageBox.Show(this, $"Agent returned exit code {process.ExitCode}. See the local log for details.\n\n{_logPath}",
                    "Installation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _status.Text = "Installation did not complete.";
            try { await File.AppendAllTextAsync(_logPath, $"[{DateTimeOffset.Now:O}] ERROR: {ex}\n", Encoding.UTF8); } catch { }
            MessageBox.Show(this, $"{ex.Message}\n\nIf needed, inspect: {_logPath}", "Installation error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { RefreshInstallState(); }
    }
}
