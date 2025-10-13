using System.Net;
using FFmpegConverter;
using Moq;
using Moq.Protected;

namespace UnitTests;

[NotInParallel]
public class UtilityTests
{
    [Test]
    public async Task DownloadSucceeds()
    {
        var url = new Uri("https://someurl.com/video.mp4");
        var content = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        var mockHandler = new Mock<HttpClientHandler>();

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new ByteArrayContent(content),
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        using var httpClient = new HttpClient(mockHandler.Object);
        Utility.HttpClientFactory = () => httpClient;

        var downloadedFile = await Utility.DownloadFileAsync(url).ConfigureAwait(false);

        await Assert.That(File.Exists(downloadedFile)).IsTrue();

        var downloadedContent = await File
            .ReadAllBytesAsync(downloadedFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Assert.That(downloadedContent).Satisfies(x => x.SequenceEqual(content), y => y.IsTrue());

        File.Delete(downloadedFile);
        responseMessage.Dispose();
    }

    [Test]
    public async Task DownloadFails()
    {
        var url = new Uri("https://someurl.com/video.mp4");
        var mockHandler = new Mock<HttpClientHandler>();

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound,
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        using var httpClient = new HttpClient(mockHandler.Object);
        Utility.HttpClientFactory = () => httpClient;

        var exception = await Assert.That(() => Utility.DownloadFileAsync(url))
            .Throws<HttpRequestException>();

        await Assert.That(exception.Message).IsEqualTo("Download failed");
        await Assert.That(exception.InnerException).IsOfType(typeof(HttpRequestException));
        await Assert.That(exception.InnerException.Message).Contains("Status code: NotFound");

        responseMessage.Dispose();
    }
}
