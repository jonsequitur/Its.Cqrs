// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reactive.Linq;
using System.Linq;
using Microsoft.Its.Recipes;

#pragma warning disable 618

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides methods for working with event handlers.
    /// </summary>
    public static class EventHandler
    {
        private static readonly ConcurrentDictionary<Type, ReflectedEventHandlerBinder[]> reflectedBinders =
            new ConcurrentDictionary<Type, ReflectedEventHandlerBinder[]>();

        private static readonly ConcurrentDictionary<Type, string> handlerTypeNames = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// Wraps all of the specified handler's event handling methods in a proxy delegate.
        /// </summary>
        /// <param name="eventHandler">The event handler.</param>
        /// <param name="proxy">The proxy.</param>
        /// <returns>A proxy for the specified event handler.</returns>
        public static IEventHandler WrapAll(
            this object eventHandler,
            Handle<IEvent> proxy)
        {
            var compositeProjector = new EventHandlerWrapper(eventHandler);

            compositeProjector.WrapAll(proxy);

            return compositeProjector;
        }

        /// <summary>
        /// Specifies a name for the event handler. 
        /// </summary>
        public static IEventHandler Named(this IEventHandler handler, string name)
        {
            handler.IfTypeIs<EventHandlerWrapper>()
                   .ThenDo(h => h.Name = name)
                   .ElseDo(() => handler.IfTypeIs<CompositeEventHandler>()
                                        .ThenDo(h => h.Name = name)
                                        .ElseDo(() => { throw new NotImplementedException($"Handlers of type {handler} do not support naming yet."); }));

            return handler;
        }

        /// <summary>
        /// Specifies a name for the consequenter. 
        /// </summary>
        public static IHaveConsequencesWhen<TEvent> Named<TEvent>(this IHaveConsequencesWhen<TEvent> consequenter, string name)
            where TEvent : IEvent
        {
            var named = consequenter as AnonymousConsequenter<TEvent>;

            if (named == null)
            {
                throw new NotImplementedException($"Handlers of type {consequenter} do not support naming yet.");
            }

            named.Name = name;

            return consequenter;
        }

        /// <summary>
        /// Specifies a name for the projector.
        /// </summary>
        public static IUpdateProjectionWhen<TEvent> Named<TEvent>(this IUpdateProjectionWhen<TEvent> projector, string name)
            where TEvent : IEvent
        {
            var named = projector as AnonymousProjector<TEvent>;

            if (named == null)
            {
                throw new NotImplementedException($"Handlers of type {projector} do not support naming yet.");
            }

            named.Name = name;

            return projector;
        }

        /// <summary>
        /// Gets the event handler binders for the specified handler.
        /// </summary>
        public static IEnumerable<IEventHandlerBinder> GetBinders(object handler) =>
            handler.IfTypeIs<IEventHandler>()
                   .Then(h => h.GetBinders())
                   .Else(() => GetBindersUsingReflection(handler));

        /// <summary>
        /// Determines whether the specfied type is an event handler type.
        /// </summary>
        /// <param name="type">The type.</param>
        public static bool IsEventHandlerType(this Type type) =>
            type.IsConsequenterType() ||
            type.IsProjectorType() ||
            type.GetInterfaces().Any(i => i == typeof (IEventHandler));

        /// <summary>
        /// Gets a short (non-namespace qualified) name for the specified event handler.
        /// </summary>
        /// <param name="handler">The event handler.</param>
        public static string Name(object handler)
        {
            EnsureIsHandler(handler);

            var named = handler as INamedEventHandler;
            if (!string.IsNullOrWhiteSpace(named?.Name))
            {
                return named.Name;
            }

            return handlerTypeNames.GetOrAdd(handler.InnerHandler().GetType(),
                                             t =>
                                             {
                                                 if (t == typeof (EventHandlerWrapper))
                                                 {
                                                     return null;
                                                 }

                                                 if (t.IsConstructedGenericType)
                                                 {
                                                     if (t.GetGenericTypeDefinition() == typeof (AnonymousConsequenter<>))
                                                     {
                                                         return "AnonymousConsequenter";
                                                     }

                                                     if (t.GetGenericTypeDefinition() == typeof (AnonymousProjector<>))
                                                     {
                                                         return "AnonymousProjector";
                                                     }
                                                 }

                                                 return t.Name;
                                             });
        }

        /// <summary>
        /// Returns the name of the event handler.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public static string FullName(object handler)
        {
            EnsureIsHandler(handler);

            var named = handler as INamedEventHandler;
            if (!string.IsNullOrWhiteSpace(named?.Name))
            {
                return named.Name;
            }

            return AttributedModelServices.GetContractName(handler.GetType());
        }

        private static void EnsureIsHandler(object handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!handler.GetType().IsEventHandlerType())
            {
                throw new ArgumentException($"Type {handler} is not an event handler.");
            }
        }

        private static IEnumerable<IEventHandlerBinder> GetBindersUsingReflection(object handler) =>
            reflectedBinders
                .GetOrAdd(handler.GetType(),
                          t =>
                          {
                              // find the handler's implementations
                              var bindings = t.ImplementedHandlerInterfaces()
                                              .Select(i => new ReflectedEventHandlerBinder(i.GetGenericArguments().First(), i))
                                              .ToArray();

                              if (bindings.Length == 0)
                              {
                                  throw new ArgumentException($"Type {handler.GetType()} does not implement any event handler interfaces.");
                              }

                              return bindings;
                          });

        internal static IEnumerable<MatchEvent> MatchesEvents(this object handler) =>
            GetBinders(handler)
                .SelectMany(binder =>
                            binder.IfTypeIs<IEventQuery>()
                                  .Then(q => q.IncludedEventTypes)
                                  .Else(() => binder.IfTypeIs<ReflectedEventHandlerBinder>()
                                                    .Then(b => MatchBasedOnEventType(b.EventType))
                                                    .Else(() => { return Enumerable.Empty<MatchEvent>(); })));

        private static IEnumerable<MatchEvent> MatchBasedOnEventType(Type eventType) =>
            Event.ConcreteTypesOf(eventType)
                 .Select(e => new MatchEvent(type: e.EventName(),
                                             streamName: e.AggregateTypeForEventType()
                                                          .IfNotNull()
                                                          .Then(AggregateType.EventStreamName)
                                                          .Else(() => MatchEvent.Wildcard)));

        /// <summary>
        /// Gets the innermost handler in a handler chain, or the handler itself if it is not chained.
        /// </summary>
        /// <param name="handler">The handler.</param> 
        public static object InnerHandler(this object handler) =>
            handler.IfTypeIs<IEventHandlerWrapper>()
                   .Then(b => b.InnerHandler.InnerHandler())
                   .Else(() => handler);

        internal static IDisposable SubscribeProjector<TEvent, TProjector>(
            TProjector handler,
            IObservable<TEvent> observable,
            IEventBus bus)
            where TEvent : IEvent
            where TProjector : class, IUpdateProjectionWhen<TEvent> =>
                observable
                    .Subscribe(
                        onNext: e =>
                        {
                            try
                            {
                                handler.UpdateProjection(e);
                            }
                            catch (Exception exception)
                            {
                                var error = new EventHandlingError(exception, handler, e);
                                bus.PublishErrorAsync(error).Wait();
                            }
                        },
                        onError: exception => bus.PublishErrorAsync(new EventHandlingError(exception, handler))
                                                 .Wait());

        internal static IDisposable SubscribeConsequences<TEvent>(
            IHaveConsequencesWhen<TEvent> handler,
            IObservable<TEvent> observable,
            IEventBus bus)
            where TEvent : IEvent => observable
                .Subscribe(
                    onNext: e =>
                    {
                        try
                        {
                            handler.HaveConsequences(e);
                        }
                        catch (Exception exception)
                        {
                            var error = new EventHandlingError(exception, handler, e);
                            bus.PublishErrorAsync(error).Wait();
                        }
                    },
                    onError: exception => bus.PublishErrorAsync(new EventHandlingError(exception, handler))
                                             .Wait());

        internal static IDisposable SubscribeDurablyAndPublishErrors<THandler, TEvent>(
            this IObservable<TEvent> events,
            THandler handler,
            Action<TEvent> handle,
            IEventBus bus)
            where TEvent : IEvent => events.Subscribe(
                onNext: e =>
                {
                    try
                    {
                        handle(e);
                    }
                    catch (Exception exception)
                    {
                        var error = new EventHandlingError(exception, handler, e);
                        bus.PublishErrorAsync(error).Wait();
                    }
                },
                onError: exception => bus.PublishErrorAsync(new EventHandlingError(exception, handler))
                                         .Wait());
    }
}