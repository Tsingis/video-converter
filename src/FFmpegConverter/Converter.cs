using System.Globalization;
using FFmpegConverter.Exceptions;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;

namespace FFmpegConverter;

public static class Converter
{
    private static readonly string[] s_executables = ["ffmpeg.exe", "ffprobe.exe"];
    private static readonly string _executablesPath = Environment.CurrentDirectory;
    private static readonly SemaphoreSlim s_ffmpegLock = new(1, 1);

    static Converter()
    {
        if (FFmpegExecutablesExist(_executablesPath))
        {
            FFmpeg.SetExecutablesPath(_executablesPath, formatprovider: CultureInfo.InvariantCulture);
        }
    }

    public static async Task<string> ConvertAsync(string inputFilePath, string outputFileDir, string outputFormat)
    {
        var outputFilePath = GetOutputFilepath(inputFilePath, outputFileDir, outputFormat);
        if (File.Exists(outputFilePath)) File.Delete(outputFilePath);

        await s_ffmpegLock.WaitAsync().ConfigureAwait(false);
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
        catch (FFmpegNotFoundException)
        {
            throw new FFmpegPathException($"FFmpeg executables not found in environment PATH or in {_executablesPath}.");
        }
        catch (Exception ex)
        {
            throw new Exceptions.ConversionException("Conversion failed.", ex);
        }
        finally
        {
            s_ffmpegLock.Release();
        }
    }

    private static bool FFmpegExecutablesExist(string targetDirectory)
    {
        var files = Directory.GetFiles(targetDirectory).Select(Path.GetFileName);
        return Array.TrueForAll(s_executables, x => files.Contains(x));
    }

    private static string GetOutputFilepath(string inputFilePath, string outputDir, string outputFormat)
    {
        var inputFile = Path.GetFileName(inputFilePath);
        var outputFilepath = Path.Join(outputDir, inputFile);
        return Path.ChangeExtension(outputFilepath, outputFormat);
    }
}
