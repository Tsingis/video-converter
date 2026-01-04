namespace IntegrationTests;

public static class GlobalSetup
{
    [Before(TestSession)]
    public static async Task Setup()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FFMPEG_PATH")))
        {
            var path = Path.Join(AppContext.BaseDirectory, "ffmpeg");
            Environment.SetEnvironmentVariable("FFMPEG_PATH", path, EnvironmentVariableTarget.Process);
        }
    }
}
