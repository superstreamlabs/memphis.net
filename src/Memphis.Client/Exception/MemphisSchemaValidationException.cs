namespace Memphis.Client.Exception
{
    public class MemphisSchemaValidationException : MemphisException
    {
        public MemphisSchemaValidationException(string err, System.Exception ex) : base(err, ex) { }
        public MemphisSchemaValidationException(string err) : base(err) { }
    }
}