using FFmpegConverter;

namespace UnitTests;

[NotInParallel]
public class VideoFormatTests
{
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
