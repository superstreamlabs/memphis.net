using System;

namespace Memphis.Client.Exception
{
    public class MemphisException : System.Exception
    {
        public MemphisException(String err) : base(err)
        {
            
        }
        
        public MemphisException(String err, System.Exception innerEx) : base(err, innerEx)
        {
            
        }
    }
}