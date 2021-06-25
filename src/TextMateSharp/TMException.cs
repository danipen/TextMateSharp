using System;

namespace TextMateSharp
{
    public class TMException : Exception
    {
        const long serialVersionUID = 1L;

        public TMException(string message) : base(message)
        {
        }

        public TMException(string message, Exception cause) : base(message, cause)
        {
        }
    }
}