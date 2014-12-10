using System;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     An exception thrown when an attempt is made to save a stale aggregate.
    /// </summary>
    [Serializable]
    public class ConcurrencyException : InvalidOperationException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConcurrencyException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected ConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="events">The events rejected due to a concurrency error.</param>
        /// <param name="innerException">The inner exception.</param>
        public ConcurrencyException(string message, IEvent[] events = null, Exception innerException = null) : base(message, innerException)
        {
            Events = events;
        }

        /// <summary>
        /// Gets or sets the events that could not be committed due to a concurrency error.
        /// </summary>
        public IEvent[] Events { get; set; }

        /// <summary>
        /// Creates and returns a string representation of the current exception.
        /// </summary>
        /// <returns>
        /// A string representation of the current exception.
        /// </returns>
        /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" PathDiscovery="*AllFiles*"/></PermissionSet>
        public override string ToString()
        {
            if (!Events.OrEmpty().Any())
            {
                return base.ToString();
            }

            return base.ToString() + Environment.NewLine + Events.ToJson();
        }
    }
}