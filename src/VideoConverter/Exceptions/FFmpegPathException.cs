namespace VideoConverter.Exceptions;

public class FFmpegPathException : Exception
{
    public FFmpegPathException()
    {
    }

    public FFmpegPathException(string message)
        : base(message)
    {
    }

    public FFmpegPathException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
