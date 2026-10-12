using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace LakersMeshAgentInstaller;

internal static class Program
{
    private static string? _embeddedPayloadDirectory;

    private static int Main(string[] args)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LakersMeshCentral", "installer.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            var uninstall = args.Any(a => string.Equals(a, "--uninstall", StringComparison.OrdinalIgnoreCase));
            var suppliedPath = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
            var agentPath = ResolveAgentPath(suppliedPath, AppContext.BaseDirectory);
            ValidateAgent(agentPath);
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(agentPath)));
            AppendLog(logPath, $"START action={(uninstall ? "fulluninstall" : "fullinstall")}; user={Environment.UserName}; source={agentPath}; sha256={hash}");

            var start = new ProcessStartInfo
            {
                FileName = agentPath, Arguments = uninstall ? "-fulluninstall" : "-fullinstall",
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
                RedirectStandardError = true, WorkingDirectory = Path.GetDirectoryName(agentPath)!
            };
            using var process = new Process { StartInfo = start };
            if (!process.Start()) throw new InvalidOperationException("Windows could not start the selected MeshCentral agent.");
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            try { process.WaitForExitAsync(timeout.Token).GetAwaiter().GetResult(); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException("The MeshCentral agent command exceeded three minutes.");
            }
            process.WaitForExit();
            AppendLog(logPath, $"EXIT code={process.ExitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}");
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            AppendLog(logPath, $"ERROR {ex}");
            return 1;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(_embeddedPayloadDirectory))
                try { Directory.Delete(_embeddedPayloadDirectory, recursive: true); } catch { }
        }
    }

    private static string ResolveAgentPath(string? suppliedPath, string baseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(suppliedPath)) return Path.GetFullPath(suppliedPath);
        using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("MeshAgentPayload.exe");
        if (payload is not null)
        {
            _embeddedPayloadDirectory = Path.Combine(Path.GetTempPath(), "LakersMeshCentral-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_embeddedPayloadDirectory);
            var extracted = Path.Combine(_embeddedPayloadDirectory, "MeshAgent.exe");
            using var output = File.Create(extracted);
            payload.CopyTo(output);
            return extracted;
        }
        var candidates = Directory.Exists(baseDirectory)
            ? Directory.GetFiles(baseDirectory, "MeshAgent*.exe", SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();
        if (candidates.Length == 1) return Path.GetFullPath(candidates[0]);
        if (candidates.Length > 1)
            throw new InvalidOperationException("More than one MeshAgent*.exe is beside this installer. Keep only the intended server-generated agent.");
        throw new FileNotFoundException("This generic build has no embedded agent. Build a one-file installer using Build-OneFileInstaller.ps1 and an agent downloaded from your own MeshCentral server.");
    }

    private static void ValidateAgent(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length < 4096 || info.Length > 250L * 1024 * 1024)
            throw new InvalidDataException("The selected file size is outside the expected range.");
        using var stream = File.OpenRead(path);
        if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z')
            throw new InvalidDataException("The selected file is not a Windows PE executable.");
    }

    private static void AppendLog(string path, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"\n[{DateTimeOffset.Now:O}] {message}\n", Encoding.UTF8);
        }
        catch { }
    }
}
