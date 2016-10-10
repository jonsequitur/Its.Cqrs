// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An abstract class from which event handlers can be derived.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.IEventHandler" />
    /// <seealso cref="Microsoft.Its.Domain.INamedEventHandler" />
    public abstract class EventHandlerBase : IEventHandler, INamedEventHandler
    {
        private readonly List<IEventHandler> eventHandlers = new List<IEventHandler>();
        private string name;

        /// <summary>
        /// Specifies an action to be taken when handling events of a specified <see cref="System.Type" />.
        /// </summary>
        protected virtual void
            On<T>(Action<T> handle) => eventHandlers.Add(new DuckTypeProjector<T>(handle));

        /// <summary>
        /// Specifies an action to be taken when handling events of a specified type, by name.
        /// </summary>
        protected virtual void
            On(string eventType, Action<dynamic> handle) => eventHandlers.Add(Projector.CreateDynamic(handle, eventType));

        /// <summary>
        /// Gets the binders for the handler.
        /// </summary>
        public IEnumerable<IEventHandlerBinder> GetBinders()
        {
            var eventHandlerBinders = eventHandlers.SelectMany(e => e.GetBinders());
            return eventHandlerBinders;
        }

        /// <summary>
        /// Gets the name of the event handler.
        /// </summary>
        public virtual string Name => name ?? (name = GetType().Name);
    }
}