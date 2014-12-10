using System;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Thrown when a scenario setup fails.
    /// </summary>
    public class ScenarioSetupException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Exception"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error. </param>
        public ScenarioSetupException(string message) : base(message)
        {
        }
    }
}