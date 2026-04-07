using System;

namespace InnoWerks.Computers.Apple
{
    public class ConfigurationOverrideException : Exception
    {
        public ConfigurationOverrideException() { }

        public ConfigurationOverrideException(string message)
            : base(message) { }

        public ConfigurationOverrideException(string message, Exception innerException) : base(message, innerException) { }

        public static void ThrowIfPresent<T>(T obj)
        {
            if (obj != null)
            {
                throw new ConfigurationOverrideException($"A device of type {typeof(T).Name} is alread present");
            }
        }
    }
}
