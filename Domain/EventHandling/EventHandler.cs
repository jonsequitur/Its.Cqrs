// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class EventHandler
    {
        private static readonly ConcurrentDictionary<Type, ReflectedEventHandlerBinder[]> reflectedBinders =
            new ConcurrentDictionary<Type, ReflectedEventHandlerBinder[]>();

        private static readonly ConcurrentDictionary<Type, string> handlerTypeNames = new ConcurrentDictionary<Type, string>();

        public static IEventHandler WrapAll(
            this object eventHandler,
            Handle<IEvent> proxy,
            string aliasForCatchup = null)
        {
            var compositeProjector = new EventHandlerWrapper(eventHandler);

            compositeProjector.WrapAll(proxy);

            return compositeProjector;
        }

        public static IEventHandler Named(this IEventHandler handler, string name)
        {
            // TODO: (Named) make this more generalizable

            handler.IfTypeIs<EventHandlerWrapper>()
                   .ThenDo(h => h.Name = name)
                   .ElseDo(() => handler.IfTypeIs<CompositeEventHandler>()
                                        .ThenDo(h => h.Name = name)
                                        .ElseDo(() => { throw new NotImplementedException(string.Format("Handlers of type {0} do not support naming yet.", handler)); }));

            return handler;
        }

        public static IHaveConsequencesWhen<TEvent> Named<TEvent>(this IHaveConsequencesWhen<TEvent> consequenter, string name)
            where TEvent : IEvent
        {
            var named = consequenter as AnonymousConsequenter<TEvent>;

            if (named == null)
            {
                // TODO: (Named) 
                throw new NotImplementedException(string.Format("Handlers of type {0} do not support naming yet.", consequenter));
            }

            named.Name = name;

            return consequenter;
        }

        public static IUpdateProjectionWhen<TEvent> Named<TEvent>(this IUpdateProjectionWhen<TEvent> projector, string name)
            where TEvent : IEvent
        {
            var named = projector as AnonymousProjector<TEvent>;

            if (named == null)
            {
                // TODO: (Named) 
                throw new NotImplementedException(string.Format("Handlers of type {0} do not support naming yet.", projector));
            }

            named.Name = name;

            return projector;
        }

        public static IEnumerable<IEventHandlerBinder> GetBinders(object handler)
        {
            return handler.IfTypeIs<IEventHandler>()
                          .Then(h => h.GetBinders())
                          .Else(() => GetBindersUsingReflection(handler));
        }

        public static bool IsEventHandlerType(this Type type)
        {
            return type.IsConsequenterType() ||
                   type.IsProjectorType() ||
                   type.GetInterfaces().Any(i => i == typeof (IEventHandler));
        }

        /// <summary>
        /// Gets a short (non-namespace qualified) name for the specified event handler.
        /// </summary>
        /// <param name="handler">The event handler.</param>
        public static string Name(object handler)
        {
            EnsureIsHandler(handler);

            var named = handler as INamedEventHandler;
            if (named != null && !string.IsNullOrWhiteSpace(named.Name))
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

        public static string FullName(object handler)
        {
            EnsureIsHandler(handler);

            var named = handler as INamedEventHandler;
            if (named != null && !string.IsNullOrWhiteSpace(named.Name))
            {
                return named.Name;
            }

            return AttributedModelServices.GetContractName(handler.GetType());
        }

        private static void EnsureIsHandler(object handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            if (!handler.GetType().IsEventHandlerType())
            {
                throw new ArgumentException(string.Format("Type {0} is not an event handler.", handler));
            }
        }

        private static IEnumerable<IEventHandlerBinder> GetBindersUsingReflection(object handler)
        {
            return reflectedBinders
                .GetOrAdd(handler.GetType(),
                          t =>
                          {
                              // find the handler's implementations
                              var bindings = t.ImplementedHandlerInterfaces()
                                              .Select(i => new ReflectedEventHandlerBinder(i.GetGenericArguments().First(), i))
                                              .ToArray();

                              if (bindings.Length == 0)
                              {
                                  throw new ArgumentException(String.Format("Type {0} does not implement any event handler interfaces.", handler.GetType()));
                              }

                              return bindings;
                          });
        }

        internal static IEnumerable<MatchEvent> MatchesEvents(this object handler)
        {
            return GetBinders(handler)
                .SelectMany(binder =>
                            binder.IfTypeIs<IEventQuery>()
                                  .Then(q => q.IncludedEventTypes)
                                  .Else(() => binder.IfTypeIs<ReflectedEventHandlerBinder>()
                                                    .Then(b => MatchBasedOnEventType(b.EventType))
                                                    .Else(() =>
                                                    {
                                                        // QUESTION: (MatchesEvents) shuld this be a wildcard match?
                                                        return Enumerable.Empty<MatchEvent>();
                                                    })));
        }

        private static IEnumerable<MatchEvent> MatchBasedOnEventType(Type eventType)
        {
            var eventTypes = Event.ConcreteTypesOf(eventType)
                                  .Select(t => t.EventName())
                                  .ToArray();

            // if an aggregate-wide subscription is present (e.g. "IEvent(CustomerAccount)"), we have to OR filter on the StreamName (a.k.a. aggregate)
            var aggregates = eventTypes
                .Select(m => Regex.Match(m, @"\((?<aggregate>[0-9a-zA-Z]+)\)", RegexOptions.ExplicitCapture))
                .Select(m => m.Groups["aggregate"].Value)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            var matchAggregates = aggregates.Select(streamName => new MatchEvent(streamName: streamName));
            var matchEvents = eventTypes.Select(type => new MatchEvent(type: type));

            return matchAggregates.Concat(matchEvents);
        }

        /// <summary>
        /// Gets the innermost handler in a handler chain, or the handler itself if it is not chained.
        /// </summary>
        /// <param name="handler">The handler.</param> 
        public static object InnerHandler(this object handler)
        {
            return handler.IfTypeIs<IEventHandlerWrapper>()
                          .Then(b => b.InnerHandler.InnerHandler())
                          .Else(() => handler);
        }

        internal static IDisposable SubscribeProjector<TEvent, TProjector>(
            TProjector handler,
            IObservable<TEvent> observable,
            IEventBus bus)
            where TEvent : IEvent
            where TProjector : class, IUpdateProjectionWhen<TEvent>
        {
            return observable
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
        }

        internal static IDisposable SubscribeConsequences<TEvent>(
            IHaveConsequencesWhen<TEvent> handler,
            IObservable<TEvent> observable,
            IEventBus bus)
            where TEvent : IEvent
        {
            return
                observable
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
        }

        internal static IDisposable SubscribeDurablyAndPublishErrors<THandler, TEvent>(
            this IObservable<TEvent> events,
            THandler handler,
            Action<TEvent> handle,
            IEventBus bus)
            where TEvent : IEvent
        {
            return
                events.Subscribe(
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
}