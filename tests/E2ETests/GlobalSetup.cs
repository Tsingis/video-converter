using System.Diagnostics;

namespace E2ETests;

public static class GlobalSetup
{
    [Before(TestSession)]
    public static async Task Setup()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FFMPEG_PATH")))
        {
            var path = Path.Join(AppContext.BaseDirectory, "ffmpeg");
            Environment.SetEnvironmentVariable("FFMPEG_PATH", path, EnvironmentVariableTarget.Process);
        }
    }

    [Before(TestSession)]
    public static async Task Setup(CancellationToken cancellationToken)
    {
        var psi = CreateBuildProcess();

        using var proc = Process.Start(psi);
        await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException("Build failed");
        }
    }

    public static ProcessStartInfo CreateRunProcess()
    {
        var projectDir = FindProject();
        var args = $"run --project \"{projectDir}\" -c Release --no-restore --no-build";
#pragma warning disable S4036 // OS commands should not rely on PATH resolution
        return new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };
#pragma warning restore S4036 // OS commands should not rely on PATH resolution
    }

    private static ProcessStartInfo CreateBuildProcess()
    {
        var projectDir = FindProject();
        var args = $"build \"{projectDir}\" -c Release";
#pragma warning disable S4036 // OS commands should not rely on PATH resolution
        return new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };
#pragma warning restore S4036 // OS commands should not rely on PATH resolution
    }

    private static string FindProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "VideoConverter")))
            {
                return Path.Combine(dir.FullName, "src", "VideoConverter");
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find project");
    }
}
