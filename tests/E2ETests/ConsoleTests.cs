using System.Diagnostics;
using System.Text;

namespace E2ETests;

[NotInParallel]
public class ConsoleTests
{
    [Test]
    public async Task QuitSucceeds()
    {
        var psi = GlobalSetup.CreateRunProcess();
        using var proc = Process.Start(psi);

        await proc.StandardInput.WriteLineAsync("quit").ConfigureAwait(false);

        await proc.StandardInput.FlushAsync().ConfigureAwait(false);

        var ct = TestContext.Current.Execution.CancellationToken;
        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        await Assert.That(proc.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task HelpOutputExists()
    {
        var psi = GlobalSetup.CreateRunProcess();
        using var proc = Process.Start(psi);

        await proc.StandardInput.WriteLineAsync("--help").ConfigureAwait(false);

        await proc.StandardInput.FlushAsync().ConfigureAwait(false);
        proc.StandardInput.Close();

        var output = new StringBuilder();
        proc.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };

        proc.BeginOutputReadLine();

        var ct = TestContext.Current.Execution.CancellationToken;
        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        await Assert.That(output.ToString())
            .Contains("<input>  The input file to convert")
            .And.Contains("-f, --format <f>  Format for the converted output file.")
            .And.Contains("-o, --output <o>  Output path for the converted file.")
            .And.Contains("q, quit           Quit the application.")
            .And.Contains("-?, -h, --help    Show help and usage information");
    }

    [Test]
    [Arguments("not-a-file", "Input file does not exist.")]
    [Arguments("http:/not-a-file", "Input uri not well formed.")]
    [Arguments("not-a-file.mp4", "Output and input formats are the same.")]
    [Arguments("not-a-file.mp4 -f mp3", "Output format is not supported.")]
    public async Task ErrorOutputs(string args, string expectedOutput)
    {
        var psi = GlobalSetup.CreateRunProcess();
        using var proc = Process.Start(psi);

        await proc.StandardInput.WriteLineAsync(args).ConfigureAwait(false);

        await proc.StandardInput.FlushAsync().ConfigureAwait(false);
        proc.StandardInput.Close();

        var output = new StringBuilder();
        proc.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };

        proc.BeginErrorReadLine();

        var ct = TestContext.Current.Execution.CancellationToken;
        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        await Assert.That(output.ToString()).Contains(expectedOutput);
    }

    public record ConversionInput(string FileInput, string OutputFormat);

    public static class ConversionInputSources
    {
        public static IEnumerable<Func<ConversionInput>> TestInputs()
        {
            yield return () =>
            {
                var path = Path.Join(AppContext.BaseDirectory, "Testvideos", "example_mp4.mp4");
                return new ConversionInput(path, "webm");
            };
            yield return () =>
            {
                var path = Environment.GetEnvironmentVariable("TESTFILE_URL");
                return new ConversionInput(path, "mp4");
            };
        }
    }

    [Test]
    [MethodDataSource(typeof(ConversionInputSources), nameof(ConversionInputSources.TestInputs))]
    public async Task ConversionSucceeds(ConversionInput input)
    {
        if (string.IsNullOrEmpty(input?.FileInput))
        {
            Skip.Test("Input file not provided");
        }

        var args = $"{input.FileInput} -f {input.OutputFormat}";

        var psi = GlobalSetup.CreateRunProcess();
        using var proc = Process.Start(psi);

        await proc.StandardInput.WriteLineAsync(args).ConfigureAwait(false);

        await proc.StandardInput.FlushAsync().ConfigureAwait(false);
        proc.StandardInput.Close();

        var output = new StringBuilder();
        proc.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };

        proc.BeginOutputReadLine();

        var ct = TestContext.Current.Execution.CancellationToken;
        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        await Assert.That(output.ToString()).Contains("Successfully converted file");
    }
}
