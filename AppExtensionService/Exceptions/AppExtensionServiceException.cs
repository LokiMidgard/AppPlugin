using System;

namespace AppExtensionService.Exceptions
{
    internal class AppExtensionServiceException : Exception
    {

        internal AppExtensionServiceException(string message) : base(message)
        {
        }
    }
}