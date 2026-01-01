namespace FFmpegConverter;

public sealed class Utility
{
    private readonly HttpClient _httpClient;

    public Utility(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> DownloadFileAsync(Uri url)
    {
        try
        {
            var file = $"{Guid.NewGuid()}_{Path.GetFileName(url?.LocalPath)}";
            var downloadPath = Path.Join(Path.GetTempPath(), file);

            var res = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            if (res.IsSuccessStatusCode)
            {
                using (var fs = new FileStream(downloadPath, FileMode.Create))
                {
                    await res.Content.CopyToAsync(fs).ConfigureAwait(false);
                    return downloadPath;
                }
            }

            throw new HttpRequestException($"Status code: {res.StatusCode}");
        }
        catch (Exception ex)
        {
            throw new HttpRequestException("Download failed", ex);
        }
    }
}
