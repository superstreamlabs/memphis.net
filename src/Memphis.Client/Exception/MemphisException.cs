namespace Memphis.Client.Exception;

public class MemphisException : System.Exception
{
    public MemphisException(String err) : base(Regex.Replace(err, @"[nN][aA][Tt][Ss]", "memphis"))
    {

    }

    public MemphisException(String err, System.Exception innerEx) : base(Regex.Replace(err, @"[nN][aA][Tt][Ss]", "memphis"), innerEx)
    {

    }
}