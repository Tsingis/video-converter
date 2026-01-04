using FFMpegCore;

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

        var beforeFiles = GetFilesWithWriteTime(path);

        Console.WriteLine($"Downloading FFmpeg executables to {path}");
        await DownloadFFmpegExecutables(path);

        var afterFiles = GetFilesWithWriteTime(path);
        var modifiedFiles = afterFiles
            .Where(x => !beforeFiles.ContainsKey(x.Key) || x.Value > beforeFiles[x.Key])
            .Select(x => Path.GetFileName(x.Key))
            .ToList();

        if (modifiedFiles.Count > 0)
        {
            Console.WriteLine($"Downloaded files: {string.Join(", ", modifiedFiles)}");
        }
        else
        {
            Console.WriteLine("Files were up-to-date");
        }
    }

    private static async Task DownloadFFmpegExecutables(string path)
    {
        var ffOptions = new FFOptions
        {
            BinaryFolder = path
        };
        await FFMpegCore.Extensions.Downloader.FFMpegDownloader.DownloadBinaries(options: ffOptions);
    }

    private static Dictionary<string, DateTime> GetFilesWithWriteTime(string path)
    {
        return Directory
            .EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
            .ToDictionary(
                x => Path.GetFullPath(x),
                x => File.GetLastWriteTimeUtc(x),
                StringComparer.OrdinalIgnoreCase
            );
    }
}
