using System;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Exception thrown when a unit test assertion fails.
    /// </summary>
    public class AssertionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AssertionException(string message) : base(message)
        {
        }
    }
}