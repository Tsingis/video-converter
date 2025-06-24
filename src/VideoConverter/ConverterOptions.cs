using CommandLine;
using CommandLine.Text;

namespace VideoConverter;

public class ConverterOptions
{
    [Option('i', "input", Required = true, HelpText = "The input file to convert.")]
    public string InputFile { get; set; }

    [Option('f', "format", Required = false, HelpText = "Format for the converted output file.")]
    public string OutputFormat { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output path for the converted file.")]
    public string OutputPath { get; set; }

    public static HelpText HandleError<T>(ParserResult<T> result)
    {
        var helpText = HelpText.AutoBuild(result, h =>
        {
            h.Heading = string.Empty;
            h.Copyright = string.Empty;
            h.AutoHelp = false;
            h.AutoVersion = false;
            return HelpText.DefaultParsingErrorsHandler(result, h);
        }, err => err);
        return helpText;
    }
}
