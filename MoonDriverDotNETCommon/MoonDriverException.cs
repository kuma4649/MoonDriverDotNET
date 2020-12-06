using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MoonDriverDotNET.Common
{
    [Serializable]
    public class MoonDriverException : Exception
    {
        public MoonDriverException()
        {
        }

        public MoonDriverException(string message) : base(message)
        {
        }

        public MoonDriverException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MoonDriverException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public MoonDriverException(string message, int row, int col) : base(string.Format(msg.get("E0300"), row, col, message))
        {
        }
    }
}
