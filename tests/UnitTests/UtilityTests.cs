using System.Net;
using Moq;
using Moq.Protected;
using VideoConverter.Common;
using Xunit;

namespace Tests;

public class UtilityTests
{
    [Fact]
    public async Task TestDownloadOk()
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

        var downloadedFile = await Utility.DownloadFileAsync(url).ConfigureAwait(true);

        Assert.True(File.Exists(downloadedFile));

        var downloadedContent = await File.ReadAllBytesAsync(downloadedFile, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(content, downloadedContent);

        File.Delete(downloadedFile);

        responseMessage.Dispose();
    }


    [Fact]
    public async Task TestDownload_Fail()
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

        var exception = await Assert
            .ThrowsAsync<HttpRequestException>(
                async () => await Utility.DownloadFileAsync(url).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Equal("Download failed", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.Contains("Status code: NotFound", exception.InnerException.Message, StringComparison.InvariantCulture);

        responseMessage.Dispose();
    }

}
