using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     Defines the root class of an aggregate.
    /// </summary>
    public interface IAggregateRoot
    {
        /// <summary>
        ///     Gets the globally unique id for this aggregate.
        /// </summary>
        Guid Id { get; }
    }
}