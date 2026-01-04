namespace FFmpegConverter;

public static class VideoFormat
{
    public const string Mp4 = "mp4";
    public const string Webm = "webm";
    public const string Gif = "gif";

    private static readonly HashSet<string> s_supportedFormats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            Mp4,
            Webm,
            Gif
        };

    public static bool IsSupportedVideoFormat(string format)
    {
        return format != null && s_supportedFormats.Contains(format);
    }
}
