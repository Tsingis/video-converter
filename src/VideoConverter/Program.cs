using FFmpegConverter;
using FFmpegConverter.Exceptions;
using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.Help;

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

        while (true)
        {
            _outputDir = _defaultOutputDir;
            _outputFormat = _defaultOutputFormat;

            Console.Write("\nType input: ");
            var input = Console.ReadLine();

            var args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var inputArg = new Argument<string>("input")
            {
                Arity = ArgumentArity.ExactlyOne,
                Description = "The input file to convert."
            };

            var formatOption = new Option<string>("-f", "--format")
            {
                Required = false,
                Description = "Format for the converted output file."
            };

            var outputOption = new Option<string>("-o", "--output")
            {
                Required = false,
                Description = "Output path for the converted file."
            };

            var quitOption = new Option<bool>("q", "quit")
            {
                Required = false,
                Description = "Quit the application."
            };

            var root = new RootCommand
            {
                inputArg,
                formatOption,
                outputOption,
                quitOption,
            };

            var version = root.Options.FirstOrDefault(o => o is VersionOption);
            if (version is not null)
            {
                root.Options.Remove(version);
            }

            var parseResult = root.Parse(args);

            var quit = parseResult.GetValue(quitOption);
            if (quit)
            {
                Environment.Exit((int)ExitCode.OK);
                break;
            }

            if (parseResult.Errors.Count > 0)
            {
                foreach (var err in parseResult.Errors)
                {
                    Console.Error.WriteLine(err.Message);
                }
                continue;
            }

            var help = parseResult.CommandResult.Children
                .OfType<System.CommandLine.Parsing.OptionResult>()
                .Any(r => r.Option is HelpOption);

            if (help)
            {
                parseResult.Invoke();
                continue;
            }

            var inFile = parseResult.CommandResult.GetValue(inputArg);
            var format = parseResult.GetValue(formatOption);
            var outputPath = parseResult.GetValue(outputOption);

            var options = new ConverterOptions
            {
                InputFile = inFile,
                OutputFormat = format,
                OutputPath = outputPath
            };

            var exitCode = HandleOptions(options);
            if (exitCode != ExitCode.Error)
            {
                HandleConvert().Wait();
            }
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
                var downloadPath = await Utility.DownloadFileAsync(new Uri(_inputFile)).ConfigureAwait(false);

                if (File.Exists(downloadPath))
                {
                    output = await Converter.ConvertAsync(downloadPath, _outputDir, _outputFormat).ConfigureAwait(false);
                    File.Delete(downloadPath);
                }
            }
            else
            {
                output = await Converter.ConvertAsync(_inputFile, _outputDir, _outputFormat).ConfigureAwait(false);
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
            var config = new ConfigurationBuilder()
                .AddJsonFile("config.json", false)
                .Build();

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
}
