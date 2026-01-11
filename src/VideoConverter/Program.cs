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

        var root = CreateRootCommand(
            out var inputArg,
            out var formatOption,
            out var outputOption,
            out var quitOption);

        while (await ReadInput() is { } input)
        {
            await ProcessInput(
                root,
                input,
                inputArg,
                formatOption,
                outputOption,
                quitOption);
        }
    }

    private static async Task ProcessInput(RootCommand root, string input,
        Argument<string> inputArg, Option<string> formatOption,
        Option<string> outputOption, Option<bool> quitOption)
    {
        var args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var parseResult = root.Parse(args);

        if (parseResult.GetValue(quitOption))
        {
            Environment.Exit((int)ExitCode.OK);
            return;
        }

        if (parseResult.Errors.Count > 0)
        {
            foreach (var err in parseResult.Errors)
            {
                await Console.Error.WriteLineAsync(err.Message);
            }
            return;
        }

        var help = parseResult.CommandResult.Children
            .OfType<System.CommandLine.Parsing.OptionResult>()
            .Any(r => r.Option is HelpOption);

        if (help)
        {
            await parseResult.InvokeAsync();
            return;
        }

        var options = new ConverterOptions
        {
            InputFile = parseResult.CommandResult.GetValue(inputArg),
            OutputFormat = parseResult.GetValue(formatOption),
            OutputPath = parseResult.GetValue(outputOption)
        };

        if (ValidOptions(options))
        {
            await HandleConvert(options);
        }
    }

    private static RootCommand CreateRootCommand(out Argument<string> inputArg, out Option<string> formatOption,
        out Option<string> outputOption, out Option<bool> quitOption)
    {
        inputArg = new Argument<string>("input")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "The input file to convert."
        };

        formatOption = new Option<string>("-f", "--format")
        {
            Description = "Format for the converted output file."
        };

        outputOption = new Option<string>("-o", "--output")
        {
            Description = "Output path for the converted file."
        };

        quitOption = new Option<bool>("q", "quit")
        {
            Description = "Quit the application."
        };

        var root = new RootCommand
        {
            inputArg,
            formatOption,
            outputOption,
            quitOption
        };

        root.Options.Remove(root.Options.OfType<VersionOption>().FirstOrDefault());

        return root;
    }

    private static async Task<string> ReadInput()
    {
        if (!Console.IsInputRedirected)
        {
            await Console.Error.WriteAsync("Type input: ");
        }

        var input = Console.ReadLine();

        return string.IsNullOrWhiteSpace(input) ? null : input;
    }

    private static bool ValidOptions(ConverterOptions options)
    {
        if (string.IsNullOrEmpty(options.OutputFormat))
        {
            options.OutputFormat = s_defaultOutputFormat;
        }

        if (!VideoFormat.IsSupportedVideoFormat(options.OutputFormat))
        {
            Console.Error.WriteErrorLine("Output format is not supported.");
            return false;
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
                    return false;
                }
            }

            var inputFormat = Path.GetExtension(options.InputFile).Replace(".", "", StringComparison.InvariantCulture);
            if (inputFormat.Equals(options.OutputFormat, StringComparison.InvariantCulture))
            {
                Console.Error.WriteErrorLine("Output and input formats are the same.");
                return false;
            }

            if (!validUrl && !File.Exists(options.InputFile))
            {
                Console.Error.WriteErrorLine("Input file does not exist.");
                return false;
            }

        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            options.OutputPath = s_defaultOutputDir;
        }

        return true;
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
            await Console.Error.WriteErrorLineAsync("Error in conversion", ex);
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
            Console.Error.WriteErrorLine("Error in config", ex);
            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
            Environment.Exit((int)ExitCode.Error);
        }
    }
}
