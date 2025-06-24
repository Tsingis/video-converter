using CommandLine;
using Microsoft.Extensions.Configuration;
using VideoConverter.Common;
using VideoConverter.Exceptions;

namespace VideoConverter;

public static class Program
{
    private static string _inputFile;
    private static string _outputDir;
    private static string _outputFormat;
    private static string _defaultOutputDir;
    private static string _defaultOutputFormat;

    public static void Main()
    {
        SetupProgram();

        Console.WriteLine("Give URL or file path and optional output format and/or output path.");
        Console.WriteLine("Default options can be set with config.json file.\n");
        Console.WriteLine("Usage: -i <input url or path> -f <output format> -o <output path>");
        Console.WriteLine("Example: -i https://video.com/video1.mp4 -f webm");

        while (true)
        {
            _outputDir = _defaultOutputDir;
            _outputFormat = _defaultOutputFormat;

            Console.Write("\nType input (q to quit): ");
            var input = Console.ReadLine();

            if (input.ToLowerInvariant().Equals("q", StringComparison.InvariantCulture))
            {
                Environment.Exit((int)ExitCode.OK);
                break;
            }

            if (!input.StartsWith("-i", StringComparison.InvariantCulture) && !string.IsNullOrEmpty(input))
            {
                input = input.Insert(0, "-i");
            }

            var options = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            using var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<ConverterOptions>(options);
            parserResult.WithParsed(opt =>
                {
                    var exitCode = HandleOptions(opt);
                    if (exitCode != ExitCode.Error)
                    {
                        HandleConvert().Wait();
                    }
                });
            parserResult.WithNotParsed(err =>
            {
                var help = ConverterOptions.HandleError(parserResult);
                Console.WriteLine(help);
            });
        }
    }

    private static ExitCode HandleOptions(ConverterOptions options)
    {
        if (options.OutputFormat is not null)
        {
            if (!VideoFormat.IsSupportedVideoFormat(options.OutputFormat))
            {
                Console.WriteLine("Output format is not supported.");
                return ExitCode.Error;
            }

            _outputFormat = options.OutputFormat;
        }

        if (!string.IsNullOrEmpty(options.InputFile))
        {
            bool validUrl = false;
            if (options.InputFile.StartsWith("http", StringComparison.InvariantCulture))
            {
                validUrl = Uri.IsWellFormedUriString(options.InputFile, UriKind.Absolute);
                if (!validUrl)
                {
                    Console.WriteLine("Input uri not well formed.");
                    return ExitCode.Error;
                }
            }

            if (!validUrl && !File.Exists(options.InputFile))
            {
                Console.WriteLine("Input file does not exist.");
                return ExitCode.Error;
            }

            var inputFormat = Path.GetExtension(options.InputFile).Replace(".", "", StringComparison.InvariantCulture);
            if (inputFormat.Equals(_outputFormat, StringComparison.InvariantCulture))
            {
                Console.WriteLine("Output and input formats are the same.");
                return ExitCode.Error;
            }

            _inputFile = options.InputFile;
        }

        if (options.OutputPath is not null)
        {
            _outputDir = options.OutputPath;
        }

        return ExitCode.OK;
    }

    private static async Task HandleConvert()
    {
        try
        {
            string output = string.Empty;
            if (Uri.IsWellFormedUriString(_inputFile, UriKind.RelativeOrAbsolute))
            {
                var downloadPath = await Utility.DownloadFileAsync(new Uri(_inputFile)).ConfigureAwait(true);

                if (File.Exists(downloadPath))
                {
                    output = await Converter.ConvertAsync(downloadPath, _outputDir, _outputFormat).ConfigureAwait(true);
                    File.Delete(downloadPath);
                }
            }
            else
            {
                output = await Converter.ConvertAsync(_inputFile, _outputDir, _outputFormat).ConfigureAwait(true);
            }

            Console.WriteLine($"Successfully conversed file {output}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error in downloading file. {ex.Message}");
        }
        catch (ConversionException ex)
        {
            Console.WriteLine($"Error in conversion. {ex.Message}");
        }
    }

    private static void SetupProgram()
    {
        try
        {
            var config = SetupConfig();
            _defaultOutputFormat = config.GetValue<string>("defaultOutputFormat");
            _defaultOutputDir = config.GetValue<string>("defaultOutputDir");

            if (!Directory.Exists(_defaultOutputDir))
            {
                var path = Path.Join(Environment.CurrentDirectory, "Output");
                Directory.CreateDirectory(path);
                _defaultOutputDir = path;
            }

            if (string.IsNullOrEmpty(_defaultOutputFormat) ||
                !VideoFormat.IsSupportedVideoFormat(_defaultOutputFormat))
            {
                _defaultOutputFormat = VideoFormat.Mp4;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in config. {ex.Message}");
            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
            Environment.Exit((int)ExitCode.Error);
        }
    }

    private static IConfiguration SetupConfig()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("config.json", false);

        return builder.Build();
    }

    private enum ExitCode
    {
        OK = 0,
        Error,
    }
}
