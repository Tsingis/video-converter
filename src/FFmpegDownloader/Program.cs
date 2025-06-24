using Xabe.FFmpeg.Downloader;

namespace FFmpeg.Downloader;

public static class Program
{
    public static void Main(string[] args)
    {
        var path = args?.Length > 0 && Directory.Exists(args[0]) ? args[0] : Environment.CurrentDirectory;
        Console.WriteLine($"Downloading FFmpeg executables to {path}");
        DownloadFFmpegExecutables(path).Wait();
        Console.WriteLine("Download finished.");
    }

    private async static Task DownloadFFmpegExecutables(string destinationPath)
    {
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Full, destinationPath).ConfigureAwait(true);
    }
}
