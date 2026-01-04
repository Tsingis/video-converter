using FFmpegConverter.Exceptions;
using FFMpegCore;
using FFMpegCore.Enums;

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

        GlobalFFOptions.Configure(options =>
        {
            options.BinaryFolder = executablesPath;
        });

        s_initialized = true;
    }

    public static async Task<string> ConvertAsync(string inputFilePath, string outputFileDir, string outputFormat)
    {
        EnsureFFmpegInitialized();

        var outputFilePath = GetOutputFilepath(inputFilePath, outputFileDir, outputFormat);
        if (File.Exists(outputFilePath)) File.Delete(outputFilePath);

        try
        {
            Action<FFMpegArgumentOptions> outputOptions = outputFormat switch
            {
                VideoFormat.Mp4 => o =>
                    o.WithVideoCodec(VideoCodec.LibX264)
                    .WithAudioCodec(AudioCodec.Aac),

                VideoFormat.Webm => o =>
                    o.WithVideoCodec(VideoCodec.LibVpx)
                    .WithAudioCodec(AudioCodec.LibVorbis),

                VideoFormat.Gif => o =>
                    o.WithCustomArgument("-vf fps=1"),

                _ => throw new VideoFormatException("Unsupported video format.")
            };

            var processor = FFMpegArguments
                .FromFileInput(inputFilePath)
                .OutputToFile(outputFilePath, true, outputOptions);

            await processor.ProcessAsynchronously().ConfigureAwait(false);
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
