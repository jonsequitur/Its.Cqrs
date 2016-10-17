// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Its.Domain
{
    internal class EventHandlerWrapper : IEventHandler,
                                         IEventHandlerWrapper,
                                         INamedEventHandler
    {
        private readonly List<IEventHandlerBinder> binders = new List<IEventHandlerBinder>();

        public EventHandlerWrapper(object innerHandler)
        {
            InnerHandler = innerHandler;
            
            var compositeProjector = innerHandler as EventHandlerWrapper;
            if (compositeProjector != null)
            {
                compositeProjector.binders.ForEach(AddBinder);
            }
            else
            {
                EventHandler.GetBinders(innerHandler)
                            .ForEach(reflectedBinder =>
                            {
                                var reflectedEventHandlerBinder = ((ReflectedEventHandlerBinder) reflectedBinder);
                                var binderType = typeof (EventHandlerWrapper<>).MakeGenericType(reflectedEventHandlerBinder.EventType);
                                var binder = Activator.CreateInstance(binderType, innerHandler);
                                AddBinder((IEventHandlerBinder) binder);
                            });
            }
            Name = EventHandler.FullName(innerHandler);
        }

        public IEnumerable<IEventHandlerBinder> GetBinders() => binders;

        public void AddBinder(IEventHandlerBinder binder) => binders.Add(binder);

        public void WrapAll(Handle<IEvent> first) => binders.ForEach(b => ((dynamic) b).Wrap(first));

        public object InnerHandler { get; }

        public string Name { get; set; }
    }
}
