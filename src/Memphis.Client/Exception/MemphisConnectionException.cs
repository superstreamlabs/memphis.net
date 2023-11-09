namespace Memphis.Client.Exception;

public class MemphisConnectionException : MemphisException
{
    public MemphisConnectionException(string err, System.Exception ex) : base(err, ex) { }
    public MemphisConnectionException(string err) : base(err) { }
}