using System;

namespace Microsoft.Its.Domain
{
    [Serializable]
    public class DomainConfigurationException : Exception
    {
        public DomainConfigurationException(string message) : base(message)
        {
        }
    }
}