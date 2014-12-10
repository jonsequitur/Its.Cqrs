using System;
using System.Runtime.Serialization;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An exception thrown when a command is unathorized.
    /// </summary>
    [Serializable]
    public class CommandAuthorizationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAuthorizationException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected CommandAuthorizationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAuthorizationException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CommandAuthorizationException(string message) : base(message)
        {
        }
    }
}