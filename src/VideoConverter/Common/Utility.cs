namespace VideoConverter.Common;

public static class Utility
{
    public static Func<HttpClient> HttpClientFactory { get; set; } = () => new HttpClient();

    public static async Task<string> DownloadFileAsync(Uri url)
    {
        try
        {
            var file = Path.GetFileName(url?.LocalPath);
            var downloadPath = Path.Join(Path.GetTempPath(), file);

            using (var client = HttpClientFactory())
            {
                var res = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead).ConfigureAwait(true);
                if (res.IsSuccessStatusCode)
                {
                    using (var fs = new FileStream(downloadPath, FileMode.Create))
                    {
                        await res.Content.CopyToAsync(fs).ConfigureAwait(true);
                        return downloadPath;
                    }
                }

                throw new HttpRequestException($"Status code: {res.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            throw new HttpRequestException("Download failed", ex);
        }
    }

    public static string GetOutputFilepath(string inputFilePath, string outputDir, string outputFormat)
    {
        var inputFile = Path.GetFileName(inputFilePath);
        var outputFilepath = Path.Join(outputDir, inputFile);
        return Path.ChangeExtension(outputFilepath, outputFormat);
    }
}
