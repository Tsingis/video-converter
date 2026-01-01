namespace VideoConverter;

internal static class ConsoleExtensions
{
    internal static void WriteSuccessLine(this TextWriter writer, string message)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var prevFg = Console.ForegroundColor;
        var prevBg = Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.Green;

        try
        {
            writer.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = prevFg;
            Console.BackgroundColor = prevBg;
        }
    }

    internal static void WriteErrorLine(this TextWriter writer, string message, Exception ex = null)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var prevFg = Console.ForegroundColor;
        var prevBg = Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        try
        {
            if (ex?.Message is not null)
            {
                message += Environment.NewLine + ex.Message;
            }
            writer.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = prevFg;
            Console.BackgroundColor = prevBg;
        }
    }

    internal static async Task WriteErrorLineAsync(this TextWriter writer, string message, Exception ex = null)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var prevFg = Console.ForegroundColor;
        var prevBg = Console.BackgroundColor;

        Console.ForegroundColor = ConsoleColor.Red;
        try
        {
            if (ex?.Message is not null)
            {
                message += Environment.NewLine + Environment.NewLine + ex.Message;
            }
            await writer.WriteLineAsync(message).ConfigureAwait(false);
        }
        finally
        {
            Console.ForegroundColor = prevFg;
            Console.BackgroundColor = prevBg;
        }
    }
}
