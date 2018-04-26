using Outputs;
using System;

namespace GZipCompressor.Exceptions
{
    public class Check
    {
        public static void That(bool condition, string message)
        {
            if (!condition)
            {
                ConsoleLogger.WriteInstructions(message);
                throw new CheckException(message);
            }
        }

        private class CheckException : Exception
        {
            public CheckException(string message) : base(message)
            {

            }
        }
    }
}
