namespace Memphis.Client.Exception
{
	public class MemphisSchemeValidationException: MemphisException
    {
        public MemphisSchemeValidationException(string err, System.Exception ex) : base(err, ex) { }
        public MemphisSchemeValidationException(string err) : base(err) { }
    }
}

