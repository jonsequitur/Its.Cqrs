using System;
using System.Linq;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Updates a projection based on events and stores the updated projection.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <remarks>Implementations should be idempotent so that the events can be replayed and the projection rebuilt as needed.</remarks>
    public interface IUpdateProjectionWhen<in TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// Updates the projection based on the incoming event.
        /// </summary>
        void UpdateProjection(TEvent @event);
    }
}