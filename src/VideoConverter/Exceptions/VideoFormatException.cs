namespace VideoConverter.Exceptions;

public class VideoFormatException : Exception
{
    public VideoFormatException()
    {
    }

    public VideoFormatException(string message)
        : base(message)
    {
    }

    public VideoFormatException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
