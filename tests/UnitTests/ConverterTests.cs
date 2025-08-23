using FFmpegConverter;

namespace UnitTests;

[NotInParallel]
public class ConverterTests
{
    const string TestVideoPath = "Testvideos";

    [Test]
    [Arguments("example_mp4.mp4", VideoFormat.Webm)]
    [Arguments("example_mp4.mp4", VideoFormat.Gif)]
    [Arguments("example_webm.webm", VideoFormat.Mp4)]
    [Arguments("example_webm.webm", VideoFormat.Gif)]
    [Arguments("example_gif.gif", VideoFormat.Mp4)]
    public async Task ConversionSucceeds(string inputFile, string outputFormat)
    {
        var inputFilePath = Path.Join(Environment.CurrentDirectory, TestVideoPath, inputFile);
        var outputFileDir = Path.Join(Environment.CurrentDirectory, TestVideoPath);

        var outputFilePath = await Converter
            .ConvertAsync(inputFilePath, outputFileDir, outputFormat)
            .ConfigureAwait(false);

        await Assert.That(File.Exists(outputFilePath)).IsTrue();
        File.Delete(outputFilePath);
    }

    [Test]
    [Arguments("mp4", true)]
    [Arguments("webm", true)]
    [Arguments("gif", true)]
    [Arguments("wmv", false)]
    public async Task IsSupportedVideoFormat(string format, bool expected)
    {
        var result = VideoFormat.IsSupportedVideoFormat(format);
        await Assert.That(result).IsEqualTo(expected);
    }
}
