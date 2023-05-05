namespace Memphis.Client.Exception
{
    public class MemphisMessageIdException : MemphisException
    {
        public MemphisMessageIdException(string err, System.Exception ex) : base(err, ex) { }
        public MemphisMessageIdException(string err) : base(err) { }
    }
}