using System.Reflection;

namespace VideoConverter;

public static class VideoFormat
{
    public const string Mp4 = "mp4";
    public const string Webm = "webm";
    public const string Gif = "gif";

    public static bool IsSupportedVideoFormat(string format)
    {
        var allowedFormats = typeof(VideoFormat).GetAllPublicConstantsValues<string>();
        return allowedFormats.Contains(format?.ToLowerInvariant());
    }

    private static List<T> GetAllPublicConstantsValues<T>(this Type type)
    {
        return type
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(x => x.IsLiteral && !x.IsInitOnly && x.FieldType == typeof(T))
        .Select(x => (T)x.GetRawConstantValue())
        .ToList();
    }
}
