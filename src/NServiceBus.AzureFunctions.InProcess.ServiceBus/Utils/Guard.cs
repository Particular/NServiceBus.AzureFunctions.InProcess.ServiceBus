namespace NServiceBus
{
    using System;

    static class Guard
    {
        public static void AgainstNullAndEmpty(string argumentName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(argumentName);
            }
        }
    }
}