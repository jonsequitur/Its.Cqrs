// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class EventExtensions
    {
        /// <summary>
        /// Returns a string representing the actor who committed the specified event.
        /// </summary>
        public static string Actor(this IEvent e)
        {
            return e.IfTypeIs<IHaveExtensibleMetada>()
                    .Then(ev => ((object) ev.Metadata)
                                    .IfTypeIs<IDictionary<string, object>>()
                                    .Then(dict =>
                                          dict.IfContains("Actor")
                                              .Then(a => a.ToString())))
                    .Else(() => null);
        }

        public static void SetActor(this IHaveExtensibleMetada e, string actor)
        {
            e.Metadata.Actor = actor;
        }

        public static void SetActor(this IHaveExtensibleMetada e, ICommand fromCommand)
        {
            e.Metadata.Actor = fromCommand.Principal
                                          .Identity
                                          .IfNotNull()
                                          .Then(i => i.Name)
                                          .ElseDefault();
        }

        /// <summary>
        /// Returns the type of the aggregate to which an event applies.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <returns>The type of the aggregate, or null if none.</returns>
        public static Type AggregateType(this IEvent @event)
        {
            return @event.GetType().AggregateTypeForEventType();
        }

        private static readonly ConcurrentDictionary<Type, Type> aggregateTypesForEventTypes = new ConcurrentDictionary<Type, Type>(); 

        /// <summary>
        /// Returns the type of the aggregate to which an event applies.
        /// </summary>
        /// <returns>The type of the aggregate, or null if none.</returns>
        public static Type AggregateTypeForEventType(this Type eventType)
        {
            return aggregateTypesForEventTypes.GetOrAdd(eventType,
                                                        t =>
                                                        {
                                                            // first, look for an IEvent<T> implementation
                                                            var aggregateType = t.GetInterfaces()
                                                                    .Where(i => i.IsGenericType)
                                                                    .Where(i => i.GetGenericTypeDefinition() == typeof (IEvent<>))
                                                                    .Select(i => i.GenericTypeArguments.First())
                                                                    .FirstOrDefault();

                                                            if (aggregateType == null && t.IsNested)
                                                            {
                                                                aggregateType = t.DeclaringType;
                                                            }

                                                            return aggregateType;
                                                        });
        }

        /// <summary>
        /// Returns the type of the aggregate to which an event applies.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <returns>The type of the aggregate, or null if none.</returns>
        public static Type AggregateType<TAggregate>(this IEvent<TAggregate> @event)
            where TAggregate : IEventSourced
        {
            return typeof (TAggregate);
        }

        private static readonly ConcurrentDictionary<Type, Func<IEvent, string>> eventNames = new ConcurrentDictionary<Type, Func<IEvent, string>>();

        /// <summary>
        /// Gets the name used to persist the event in the event store.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <remarks>By default, this is the event class's type name, with no namespace. This name can be specified by adding the EventNameAttribute to the class.</remarks>
        /// <returns>A string representing the class's name.</returns>
        public static string EventName(this IEvent @event)
        {
            return eventNames.GetOrAdd(@event.GetType(), t =>
            {
                Func<IEvent, string> defaultName = e => e.GetType().EventName();

                if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IScheduledCommand<>)))
                {
                    return e => defaultName(e) + ":" + ((dynamic) e).Command.GetType().Name;
                }

                if (t == typeof (DynamicEvent))
                {
                    return e => ((DynamicEvent) e).EventTypeName;
                }

                return defaultName;
            })(@event);
        }

        private static readonly ConcurrentDictionary<Type, string> eventNames2 = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// Gets the name used to persist the event in the event store.
        /// </summary>
        /// <remarks>By default, this is the event class's type name, with no namespace. This name can be specified by adding the EventNameAttribute to the class.</remarks>
        /// <returns>A string representing the class's name.</returns>
        public static string EventName(this Type eventType)
        {
            // TODO: (EventExtensions) differentiate these two methods in terms of use cases. it's potentially misleading since they can return different values
            return eventNames2.GetOrAdd(eventType, t => t.GetCustomAttributes(false)
                                                         .OfType<EventNameAttribute>()
                                                         .FirstOrDefault()
                                                         .IfNotNull()
                                                         .Then(att => att.EventName)
                                                         .Else(() =>
                                                         {
                                                             if (t.IsGenericType)
                                                             {
                                                                 var contractName = AttributedModelServices.GetContractName(t);
                                                                 // e.g. Microsoft.Its.Domain.Event(Microsoft.Its.SomeAggregate) --> Event(SomeAggregate)
                                                                 contractName = new Regex(@"([0-9\w\+]+\.)|([0-9\w\+]+\+)([\(\)]*)").Replace(contractName, "$3");
                                                                 return contractName;
                                                             }
                                                             return t.Name;
                                                         }));
        }

        public static T GetAggregate<T>(this Event<T> @event) where T : class, IEventSourced
        {
            // TODO: (GetAggregate) this is possibly awkward to use, e.g. expects the handler to know that the aggregate was sourced.
            return ((object) @event.Metadata)
                .IfTypeIs<IDictionary<string, object>>()
                .And()
                .IfContains("Aggregate")
                .And()
                .IfTypeIs<T>()
                .Else<T>(() =>
                {
                    var repository = Configuration.Current.Container.Resolve<IEventSourcedRepository<T>>();
                    return repository.GetLatest(@event.AggregateId);
                });
        }

        internal static void SetAggregate(this IEvent @event, IEventSourced aggregate)
        {
            @event.IfTypeIs<IHaveExtensibleMetada>()
                  .ThenDo(e => ((object) e.Metadata)
                                   .IfTypeIs<IDictionary<string, object>>()
                                   .ThenDo(metadata => { metadata["Aggregate"] = aggregate; }));
        }
    }
}
