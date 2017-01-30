using System;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Base class for exceptions thrown by DXA templating code.
    /// </summary>
    public class DxaException : ApplicationException
    {
        public DxaException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
