using FFmpegConverter;

namespace IntegrationTests;

public class VideoFormatTests
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
        var outputFileDir = Path.GetTempPath();

        var outputFilePath = await Converter
            .ConvertAsync(inputFilePath, outputFileDir, outputFormat)
            .ConfigureAwait(false);

        await Assert.That(File.Exists(outputFilePath)).IsTrue();
        File.Delete(outputFilePath);
    }
}
