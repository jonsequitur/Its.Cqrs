using System;

namespace Microsoft.Its.Domain
{
    public interface IEventHandlerBinder
    {
        /// <summary>
        /// Subscribes the specified handler to the event bus.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="bus">The bus.</param>
        IDisposable SubscribeToBus(object handler, IEventBus bus);
    }
}