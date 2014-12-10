using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Handles events published on an <see cref="IEventBus" />.
    /// </summary>
    public interface IEventHandler
    {
        IEnumerable<IEventHandlerBinder> GetBinders();
    }
}