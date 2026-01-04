[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Tsingis_video-converter-console&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Tsingis_video-converter-console) [![Release Status](https://github.com/tsingis/video-converter-console/actions/workflows/release.yml/badge.svg)](https://github.com/tsingis/video-converter-console/actions/workflows/release.yml)

# Video converter

What is it?

-   Conversions between common video formats
    -   Supported output formats are mp4, webm and gif
-   Input as file url or path

Requirements:

-   [FFmpeg](https://ffmpeg.org/download.html) executables downloaded
-   (Optional) use _FFmpegDownloader_ to download required executables
-   FFmpeg executables directory set to `FFMPEG_PATH` environment variable

Configuration:

-   Change default values for output directory and default output format in the `config.json` file. Example below:

```json
{
    "defaultOutputDir": "%USERPROFILE%\\Downloads",
    "defaultOutputFormat": "mp4"
}
```

Tools used:

-   .NET SDK
