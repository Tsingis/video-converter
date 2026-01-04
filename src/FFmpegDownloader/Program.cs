using Xabe.FFmpeg.Downloader;

namespace FFmpeg.Downloader;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

public static class Program
{
    public static async Task Main(string[] args)
    {
        var path = args?.Length > 0
            ? args[0]
            : AppContext.BaseDirectory;

        Directory.CreateDirectory(path);

        Console.WriteLine($"Downloading FFmpeg executables to {path}");
        await DownloadFFmpegExecutables(path);
        Console.WriteLine("Download finished.");
    }

    private static async Task DownloadFFmpegExecutables(string destinationPath)
    {
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, destinationPath);
    }
}
