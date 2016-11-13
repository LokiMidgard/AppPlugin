using System;

namespace AppExtensionService.Exceptions
{
    internal class ExtensionException : Exception
    {

        internal ExtensionException(string message) : base(message)
        {
        }

    }
}