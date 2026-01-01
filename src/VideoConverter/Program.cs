using FFmpegConverter;
using FFmpegConverter.Exceptions;
using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.Help;

namespace VideoConverter;

public static class Program
{
    private static string s_defaultOutputDir;
    private static string s_defaultOutputFormat;

    public static void Main()
    {
        SetupProgram();

        while (true)
        {
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
                HandleConvert(options).Wait();
            }
        }
    }

    private static ExitCode HandleOptions(ConverterOptions options)
    {
        if (string.IsNullOrEmpty(options.OutputFormat))
        {
            options.OutputFormat = s_defaultOutputFormat;
        }

        if (!VideoFormat.IsSupportedVideoFormat(options.OutputFormat))
        {
            Console.WriteLine("Output format is not supported.");
            return ExitCode.Error;
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
            if (inputFormat.Equals(options.OutputFormat, StringComparison.InvariantCulture))
            {
                Console.WriteLine("Output and input formats are the same.");
                return ExitCode.Error;
            }
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            options.OutputPath = s_defaultOutputDir;
        }

        return ExitCode.OK;
    }

    private static async Task HandleConvert(ConverterOptions options)
    {
        try
        {
            string output = string.Empty;
            if (Uri.IsWellFormedUriString(options.InputFile, UriKind.RelativeOrAbsolute))
            {
                var downloadPath = await Utility.DownloadFileAsync(new Uri(options.InputFile)).ConfigureAwait(false);

                if (File.Exists(downloadPath))
                {
                    output = await Converter.ConvertAsync(downloadPath, options.OutputPath, options.OutputFormat).ConfigureAwait(false);
                    File.Delete(downloadPath);
                }
            }
            else
            {
                output = await Converter.ConvertAsync(options.InputFile, options.OutputPath, options.OutputFormat).ConfigureAwait(false);
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
                .AddJsonFile("config.json", true)
                .Build()
                .Get<ConfigurationOptions>();

            s_defaultOutputFormat = config?.DefaultOutputFormat;
            s_defaultOutputDir = config?.DefaultOutputDir;

            if (!Directory.Exists(s_defaultOutputDir))
            {
                var path = Path.Join(Environment.CurrentDirectory, "Output");
                Directory.CreateDirectory(path);
                s_defaultOutputDir = path;
            }

            if (string.IsNullOrEmpty(s_defaultOutputFormat) ||
                !VideoFormat.IsSupportedVideoFormat(s_defaultOutputFormat))
            {
                s_defaultOutputFormat = VideoFormat.Mp4;
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
