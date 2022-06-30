using System;

namespace BlockSignatureCalculator.Common
{
    public class CustomException : Exception
    {
        public CustomException(string message) : base(message)
        {
        }
    }
}
