using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides access to time for the domain.
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Gets the current time.
        /// </summary>
        DateTimeOffset Now();
    }
}