using VideoConverter;
using Xunit;

namespace Tests;

public class ConverterTests
{
    const string TestVideoPath = "Testvideos";

    [Theory]
    [InlineData("example_mp4.mp4", VideoFormat.Webm)]
    [InlineData("example_mp4.mp4", VideoFormat.Gif)]
    [InlineData("example_webm.webm", VideoFormat.Mp4)]
    [InlineData("example_webm.webm", VideoFormat.Gif)]
    [InlineData("example_gif.gif", VideoFormat.Mp4)]
    public async Task TestConversion(string inputFile, string outputFormat)
    {
        var inputFilePath = Path.Join(TestVideoPath, inputFile);
        var outputFileDir = Path.Join(Environment.CurrentDirectory, TestVideoPath);

        var outputFilePath = await Converter.ConvertAsync(inputFilePath, outputFileDir, outputFormat).ConfigureAwait(true);

        Assert.True(File.Exists(outputFilePath));
        File.Delete(outputFilePath);
    }

    [Theory]
    [InlineData("mp4", true)]
    [InlineData("webm", true)]
    [InlineData("gif", true)]
    [InlineData("wmv", false)]
    public void TestSupportedVideoFormat(string format, bool isSupported)
    {
        var result = VideoFormat.IsSupportedVideoFormat(format);
        Assert.Equal(isSupported, result);
    }
}
