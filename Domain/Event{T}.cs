// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An domain event pertaining to a specific aggregate type.
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    public abstract class Event<TAggregate> : Event, IEvent<TAggregate>
        where TAggregate : IEventSourced
    {
        private static readonly Type[] knownTypes =
            Discover.ConcreteTypesDerivedFrom(typeof (IEvent<TAggregate>))
                    .Concat(new[]
                    {
                        // add known generic variants for TAggregate
                        typeof (CommandScheduled<TAggregate>),
                        typeof (Annotated<TAggregate>)
                    })
                    .ToArray();

        /// <summary>
        ///     Gets all known event types (derived from <see cref="IEvent{T}" />) in the loaded assemblies.
        /// </summary>
        public static Type[] KnownTypes
        {
            get
            {
                return knownTypes;
            }
        }

        /// <summary>
        ///     Gets all known event handler types in the loaded assemblies.
        /// </summary>
        public static Type[] KnownHandlerTypes
        {
            get
            {
                // TODO: (KnownHandlerTypes) cache?
                return Discover.ConcreteTypesOfGenericInterfaces(HandlerGenericTypeDefinitions)
                               .Where(t =>
                               {
                                   var handlerInterfaces = t.GetInterfaces()
                                                            .Where(i => i.IsGenericType &&
                                                                        HandlerGenericTypeDefinitions.Contains(i.GetGenericTypeDefinition()));

                                   return handlerInterfaces.Any(handlerInterface =>
                                   {
                                       var genericArg = handlerInterface.GetGenericArguments().Single();

                                       if (genericArg == typeof (IEvent))
                                       {
                                           // the handler handles IEvent
                                           return true;
                                       }

                                       if ((typeof (IEvent).IsAssignableFrom(genericArg) && !genericArg.IsGenericType))
                                       {
                                           // e.g. Event
                                           return true;
                                       }

                                       return genericArg.GetInterfaces()
                                                        .Concat(new[] { genericArg })
                                                        .Any(
                                                            eventInterface =>
                                                            eventInterface.IsGenericType &&
                                                            eventInterface.GetGenericTypeDefinition() ==
                                                            typeof (IEvent<>) &&
                                                            eventInterface.GetGenericArguments()
                                                                          .Single() == typeof (TAggregate));
                                   });
                               })
                               .ToArray();
            }
        }

        /// <summary>
        ///     Updates an aggregate to a new state.
        /// </summary>
        /// <param name="aggregate">The aggregate to be updated.</param>
        /// <remarks>This method is called when materializing an aggregate from an event stream.</remarks>
        public abstract void Update(TAggregate aggregate);
    }
}
