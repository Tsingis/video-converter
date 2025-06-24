using System.Globalization;
using VideoConverter.Common;
using VideoConverter.Exceptions;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;

namespace VideoConverter;

public static class Converter
{
    private static readonly string[] _executables = ["ffmpeg.exe", "ffprobe.exe", "ffplay.exe"];
    private static readonly string _executablesPath = Environment.CurrentDirectory;

    public static async Task<string> ConvertAsync(string inputFilePath, string outputFileDir, string outputFormat)
    {
        if (FFmpegExecutablesExist(_executablesPath))
        {
            FFmpeg.SetExecutablesPath(_executablesPath, formatprovider: CultureInfo.InvariantCulture);
        }

        var outputFilePath = Utility.GetOutputFilepath(inputFilePath, outputFileDir, outputFormat);
        if (File.Exists(outputFilePath)) File.Delete(outputFilePath);

        try
        {
            IConversion conversion = outputFormat switch
            {
                VideoFormat.Mp4 => await FFmpeg.Conversions.FromSnippet.ToMp4(inputFilePath, outputFilePath).ConfigureAwait(true),
                VideoFormat.Webm => await FFmpeg.Conversions.FromSnippet.ToWebM(inputFilePath, outputFilePath).ConfigureAwait(true),
                VideoFormat.Gif => await FFmpeg.Conversions.FromSnippet.ToGif(inputFilePath, outputFilePath, 1).ConfigureAwait(true),
                _ => throw new VideoFormatException("Unsupported video format."),
            };
            await conversion.Start().ConfigureAwait(true);
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
    }

    private static bool FFmpegExecutablesExist(string targetDirectory)
    {
        var files = Directory.GetFiles(targetDirectory).Select(Path.GetFileName);
        return Array.TrueForAll(_executables, x => files.Contains(x));
    }
}
