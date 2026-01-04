using System.Globalization;
using FFmpegConverter.Exceptions;
using Xabe.FFmpeg;

namespace FFmpegConverter;

public static class Converter
{
    private static bool s_initialized;

    private static void EnsureFFmpegInitialized()
    {
        if (s_initialized)
        {
            return;
        }

        var executablesPath =
            Environment.GetEnvironmentVariable("FFMPEG_PATH")
            ?? Path.Join(Environment.CurrentDirectory, "ffmpeg");

        if (!FFmpegExecutablesExist(executablesPath))
        {
            throw new FFmpegPathException($"FFmpeg executables not found in env 'FFMPEG_PATH' or {executablesPath}");
        }

        FFmpeg.SetExecutablesPath(executablesPath, formatprovider: CultureInfo.InvariantCulture);
        s_initialized = true;
    }

    public static async Task<string> ConvertAsync(string inputFilePath, string outputFileDir, string outputFormat)
    {
        EnsureFFmpegInitialized();

        var outputFilePath = GetOutputFilepath(inputFilePath, outputFileDir, outputFormat);
        if (File.Exists(outputFilePath)) File.Delete(outputFilePath);

        try
        {
            IConversion conversion = outputFormat switch
            {
                VideoFormat.Mp4 => await FFmpeg.Conversions.FromSnippet.ToMp4(inputFilePath, outputFilePath).ConfigureAwait(false),
                VideoFormat.Webm => await FFmpeg.Conversions.FromSnippet.ToWebM(inputFilePath, outputFilePath).ConfigureAwait(false),
                VideoFormat.Gif => await FFmpeg.Conversions.FromSnippet.ToGif(inputFilePath, outputFilePath, 1).ConfigureAwait(false),
                _ => throw new VideoFormatException("Unsupported video format."),
            };
            await conversion.Start().ConfigureAwait(false);
            return outputFilePath;
        }
        catch (Exception ex)
        {
            throw new ConversionException("Conversion failed.", ex);
        }
    }

    private static bool FFmpegExecutablesExist(string targetDirectory)
    {
        string[] executables = OperatingSystem.IsWindows()
            ? ["ffmpeg.exe", "ffprobe.exe"]
            : ["ffmpeg", "ffprobe"];
        return executables.All(name => File.Exists(Path.Combine(targetDirectory, name)));
    }

    private static string GetOutputFilepath(string inputFilePath, string outputDir, string outputFormat)
    {
        var inputFile = Path.GetFileName(inputFilePath);
        var outputFilepath = Path.Join(outputDir, inputFile);
        return Path.ChangeExtension(outputFilepath, outputFormat);
    }
}
