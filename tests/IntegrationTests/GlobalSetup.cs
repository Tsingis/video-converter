namespace IntegrationTests;

public static class GlobalSetup
{
    [Before(TestSession)]
    public static async Task Setup()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        Environment.SetEnvironmentVariable("FFMPEG_PATH", path, EnvironmentVariableTarget.Process);
    }
}
