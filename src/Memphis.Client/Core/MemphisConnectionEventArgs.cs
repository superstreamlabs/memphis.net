namespace Memphis.Client;

public class MemphisConnectionEventArgs
{
    public MemphisException? Error { get; set; }

    internal MemphisConnectionEventArgs(MemphisException? error = null)
    {
        Error = error;
    }


    public static implicit operator MemphisConnectionEventArgs(ConnEventArgs natsEventArgs)
    {
        if (natsEventArgs is { Error: { } })
        {
            var error = new MemphisException(natsEventArgs.Error.Message, natsEventArgs.Error);
            return new(error);
        }
        return new();
    }
}
