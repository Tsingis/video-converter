using FFmpegConverter;
using FFmpegConverter.Exceptions;
using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.Help;

namespace VideoConverter;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

public static class Program
{
    private static string s_defaultOutputDir;
    private static string s_defaultOutputFormat;

    private static readonly HttpClient s_httpClient = new();
    private static readonly Utility s_utility = new(s_httpClient);

    public static async Task Main()
    {
        SetupProgram();

        while (true)
        {
            if (!Console.IsInputRedirected)
            {
                await Console.Error.WriteAsync("Type input: ");
            }

            var input = Console.ReadLine();

            if (input is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

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
                    await Console.Error.WriteLineAsync(err.Message);
                }
                continue;
            }

            var help = parseResult.CommandResult.Children
                .OfType<System.CommandLine.Parsing.OptionResult>()
                .Any(r => r.Option is HelpOption);

            if (help)
            {
                await parseResult.InvokeAsync();
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
            if (exitCode == ExitCode.Error)
            {
                continue;
            }

            await HandleConvert(options);
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
            Console.Error.WriteErrorLine("Output format is not supported.");
            return ExitCode.Error;
        }

        if (!string.IsNullOrEmpty(options.InputFile))
        {
            bool validUrl = false;
            if (options.InputFile.StartsWith("http", StringComparison.InvariantCulture))
            {
                validUrl = Uri.IsWellFormedUriString(options.InputFile, UriKind.RelativeOrAbsolute);
                if (!validUrl)
                {
                    Console.Error.WriteErrorLine("Input uri not well formed.");
                    return ExitCode.Error;
                }
            }

            var inputFormat = Path.GetExtension(options.InputFile).Replace(".", "", StringComparison.InvariantCulture);
            if (inputFormat.Equals(options.OutputFormat, StringComparison.InvariantCulture))
            {
                Console.Error.WriteErrorLine("Output and input formats are the same.");
                return ExitCode.Error;
            }

            if (!validUrl && !File.Exists(options.InputFile))
            {
                Console.Error.WriteErrorLine("Input file does not exist.");
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
            if (Uri.TryCreate(options.InputFile, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                var downloadPath = await s_utility.DownloadFileAsync(uri);

                if (File.Exists(downloadPath))
                {
                    output = await Converter.ConvertAsync(downloadPath, options.OutputPath, options.OutputFormat);
                    File.Delete(downloadPath);
                }
            }
            else
            {
                output = await Converter.ConvertAsync(options.InputFile, options.OutputPath, options.OutputFormat);
            }

            Console.Out.WriteSuccessLine($"Successfully converted file {output}");
        }
        catch (HttpRequestException ex)
        {
            await Console.Error.WriteErrorLineAsync("Error in downloading file", ex);
        }
        catch (ConversionException ex)
        {
            await Console.Error.WriteErrorLineAsync($"Error in conversion", ex);
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
                var path = Path.Join(AppContext.BaseDirectory, "Output");
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
            Console.Error.WriteErrorLine($"Error in config", ex);
            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
            Environment.Exit((int)ExitCode.Error);
        }
    }
}
