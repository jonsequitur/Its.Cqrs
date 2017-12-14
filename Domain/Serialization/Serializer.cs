// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Serialization
{
    /// <summary>
    /// Provides methods for serialization.
    /// </summary>
    public static class Serializer
    {
        private static readonly Lazy<Func<JsonSerializerSettings, JsonSerializerSettings>> cloneSettings =
            new Lazy<Func<JsonSerializerSettings, JsonSerializerSettings>>(
                () => MappingExpression.From<JsonSerializerSettings>
                                       .ToNew<JsonSerializerSettings>()
                                       .Compile());

        private static readonly Lazy<JsonSerializerSettings> diagnosticSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                ContractResolver = new OptionalContractResolver(),
                DefaultValueHandling = DefaultValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Error = (sender, args) => args.ErrorContext.Handled = true
            };

            Settings.Converters.Add(new OptionalConverter());
            Settings.Converters.Add(new UriConverter());

            return jsonSerializerSettings;
        }); 

        private static readonly ConcurrentDictionary<string, Func<StoredEvent, IEvent>> deserializers = new ConcurrentDictionary<string, Func<StoredEvent, IEvent>>();
        
        private static JsonSerializerSettings settings;

        internal static bool AreDefaultSerializerSettingsConfigured;

        /// <summary>
        /// Gets or sets the default settings for the JSON serializer.
        /// </summary>
        public static JsonSerializerSettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                if (value == null)
                {
                    ConfigureDefault();
                    return;
                }

                settings = value;
                AreDefaultSerializerSettingsConfigured = false;
            }
        }

        static Serializer()
        {
            ConfigureDefault();
        }

        /// <summary>
        /// Configures the <see cref="Serializer.Settings" /> instance to the default.
        /// </summary>
        public static void ConfigureDefault()
        {
            Settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new OptionalContractResolver(),
                DefaultValueHandling = DefaultValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateParseHandling = DateParseHandling.DateTimeOffset
            };

            Settings.Converters.Add(new OptionalConverter());
            Settings.Converters.Add(new UriConverter());

            AreDefaultSerializerSettingsConfigured = true;
        }

        /// <summary>
        /// Adds a conversion to be be used when serializing and seserializing between a JSON value and <typeparamref name="T" />.
        /// </summary>
        public static void AddPrimitiveConverter<T>(
            Func<T, object> serialize,
            Func<object, T> deserialize)
        {
            if (!Settings.Converters.Any(c => c is PrimitiveConverter<T>))
            {
                Settings.Converters.Add(new PrimitiveConverter<T>(serialize, deserialize));
            }
        }

        /// <summary>
        /// Clones the <see cref="Serializer.Settings" /> instance.
        /// </summary>
        public static JsonSerializerSettings CloneSettings()
        {
            return cloneSettings.Value(Settings);
        }

        /// <summary>
        /// Serializes the specified object to JSON using <see cref="Serializer.Settings" />.
        /// </summary>
        public static string ToJson<T>(this T obj, Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(obj, formatting, Settings);
        }

        internal static string ToDiagnosticJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, diagnosticSettings.Value);
        }

        /// <summary>
        /// Deserializes an instance of the specified type from JSON using <see cref="Serializer.Settings" />.
        /// </summary>
        public static T FromJsonTo<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        /// <summary>
        /// Deserializes a domain event.
        /// </summary>
        /// <param name="aggregateName">Name of the aggregate.</param>
        /// <param name="eventName">Name of the event type.</param>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="sequenceNumber">The sequence number of the event.</param>
        /// <param name="timestamp">The timestamp of the event.</param>
        /// <param name="body">The body of the event.</param>
        /// <param name="uniqueEventId">The unique event identifier.</param>
        /// <param name="serializerSettings">The serializer settings used when deserializing the event body.</param>
        /// <param name="deserialize">Deserializes the event.</param>
        /// <param name="etag">The ETag of the event.</param>
        /// <returns></returns>
        public static IEvent DeserializeEvent(
            string aggregateName,
            string eventName,
            Guid aggregateId,
            long sequenceNumber,
            DateTimeOffset timestamp,
            string body,
            dynamic uniqueEventId = null,
            JsonSerializerSettings serializerSettings = null,
            DeserializeEvent deserialize = null,
            string etag = null)
        {
            var deserializerKey = aggregateName + ":" + eventName;

            deserialize = deserialize ?? BuildDeserializer(serializerSettings);

            var deserializer = deserializers.GetOrAdd(
                deserializerKey,
                _ => GetDeserializer(aggregateName, eventName, deserialize));

            var @event =
                deserializer(
                    new StoredEvent
                        {
                            StreamName = aggregateName,
                            EventTypeName = eventName,
                            Body = body,
                            AggregateId = aggregateId,
                            SequenceNumber = sequenceNumber,
                            Timestamp = timestamp,
                            ETag = etag
                        });

            if (uniqueEventId != null)
            {
                @event.IfTypeIs<IHaveExtensibleMetada>()
                      .Then(e => e.Metadata.AbsoluteSequenceNumber = uniqueEventId);
            }

            return @event;
        }

        private static DeserializeEvent BuildDeserializer(JsonSerializerSettings serializerSettings)
        {
            serializerSettings = serializerSettings ?? Settings;
            DeserializeEvent result = delegate (string input, Type type)
            {
                if (type == null)
                {
                    return JsonConvert.DeserializeObject(input, serializerSettings);
                }
                else
                {
                    return JsonConvert.DeserializeObject(input, type, serializerSettings);
                }
            };

            return result;
        }

        private static Func<StoredEvent, IEvent> GetDeserializer(
            string aggregateName,
            string eventName,
            DeserializeEvent deserialize)
        {
            var aggregateType = FindAggregateType(aggregateName);

            // some events contain specialized names, e.g. CommandScheduled:DoSomething. the latter portion is not interesting from a serialization standpoint, so we strip it off.
            var eventNameComponents = eventName.Split(':');

            var eventType = FindEventType(
                aggregateName,
                aggregateType,
                eventNameComponents.First());

            if (aggregateType == null &&
                eventType == null)
            {
                return DeserializeAsDynamicEvent(deserialize);
            }

            if (eventType == null)
            {
                // even if the domain no longer cares about some old event type, anonymous events are returned as placeholders in the EventSequence
                return DeserializeAsAnonymousEvent(
                    deserialize,
                    aggregateType);
            }

            if (typeof (IScheduledCommand).IsAssignableFrom(eventType))
            {
                var commandType = Command.FindType(
                    aggregateType,
                    eventNameComponents.Last());

                if (commandType == null)
                {
                    return DeserializeAsAnonymousEvent(
                        deserialize,
                        aggregateType);
                }
            }

            return DeserializeAsEventType(deserialize, eventType);
        }

        private static Type FindAggregateType(string aggregateName)
        {
            var aggregateType = AggregateType.KnownTypes
                                             .SingleOrDefault(
                                                 t => t.Name == aggregateName);
            return aggregateType;
        }

        private static Type FindEventType(
            string aggregateName, 
            Type aggregateType, 
            string actualEventName)
        {
            Type eventType = null;

            if (aggregateType != null)
            {
                eventType = FindEventTType(aggregateType, actualEventName);
            }

            if (eventType == null)
            {
                eventType = FindEventType(aggregateName, actualEventName);
            }
           
            return eventType;
        }

        private static Type FindEventType(
            string aggregateName,
            string eventName)
        {
            var candidateTypes = Event.KnownTypes()
                                      .Where(t => t.Name == eventName &&
                                                  t.IsNested &&
                                                  t.DeclaringType?.Name == aggregateName)
                                      .ToArray();

            if (candidateTypes.Length == 1)
            {
                return candidateTypes[0];
            }

            return null;
        }

        private static Func<StoredEvent, IEvent> DeserializeAsEventType(
            DeserializeEvent deserialize,
            Type eventType)
        {
            if (typeof (Event).IsAssignableFrom(eventType))
            {
                return e =>
                {
                    var deserialized = (Event) deserialize(e.Body, eventType);

                    deserialized.AggregateId = e.AggregateId;
                    deserialized.SequenceNumber = e.SequenceNumber;
                    deserialized.Timestamp = e.Timestamp;
                    deserialized.ETag = e.ETag;

                    return deserialized;
                };
            }

            return e =>
            {
                var deserialized = (IEvent) deserialize(e.Body, eventType);

                JsonConvert.PopulateObject(e.ToJson(), deserialized);

                return deserialized;
            };
        }

        private static Func<StoredEvent, IEvent> DeserializeAsDynamicEvent(
            DeserializeEvent deserialize)
        {
            return e =>
            {
                dynamic deserializeObject = deserialize(e.Body);
                var dynamicEvent = new DynamicEvent(deserializeObject)
                {
                    EventStreamName = e.StreamName,
                    EventTypeName = e.EventTypeName,
                    AggregateId = e.AggregateId,
                    SequenceNumber = e.SequenceNumber,
                    Timestamp = e.Timestamp,
                    ETag = e.ETag
                };

                return dynamicEvent;
            };
        }

        private static Func<StoredEvent, IEvent> DeserializeAsAnonymousEvent(
            DeserializeEvent deserialize,
            Type aggregateType)
        {
            return e =>
            {
                    var anonymousEventType = typeof(AnonymousEvent<>).MakeGenericType(aggregateType);
                    var anonymousEvent = (Event) deserialize(e.Body, anonymousEventType);
                    anonymousEvent.AggregateId = e.AggregateId;
                    anonymousEvent.SequenceNumber = e.SequenceNumber;
                    anonymousEvent.Timestamp = e.Timestamp;
                    anonymousEvent.ETag = e.ETag;
                    ((dynamic) anonymousEvent).Body = e.Body;

                    return anonymousEvent;
                };
        }

        private static Type FindEventTType(
            Type aggregateType, 
            string actualEventName)
        {
            IEnumerable<Type> eventTypes = typeof (Event<>).MakeGenericType(aggregateType).Member().KnownTypes;

            var eventType = eventTypes.SingleOrDefault(t =>
                                                       t.GetCustomAttributes(false)
                                                        .OfType<EventNameAttribute>()
                                                        .FirstOrDefault()
                                                        .IfNotNull()
                                                        .Then(
                                                            att => att.EventName == actualEventName)
                                                        .Else(() =>
                                                              // strip off generic specifications from the type name 
                                                              t.Name.Split('`').First() == actualEventName));

            return eventType;
        }

        /// <summary>
        /// Deserializes a JSON array to a sequence of events.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static IEnumerable<IEvent> FromJsonToEvents(string json)
        {
            JArray jsonEvents = JArray.Parse(json);

            return jsonEvents.OfType<JObject>()
                             .Select(o =>
                                     new
                                     {
                                         o.Properties().Single().Name,
                                         o.Properties().Single().Value
                                     })
                             .Select(nameAndBody =>
                             {
                                 var split = nameAndBody.Name.Split('.');
                                 var aggregateTypeName = split[0];
                                 var eventTypeName = split[1];
                                 var aggregateType = AggregateType.KnownTypes
                                                                  .SingleOrDefault(t => t.Name == aggregateTypeName);

                                 return new
                                 {
                                     EventType = Event.KnownTypesForAggregateType(aggregateType)
                                                      .SingleOrDefault(t => Equals(t.Name, eventTypeName)),
                                     Body = nameAndBody.Value.ToString()
                                 };
                             })
                             .Where(typeAndBody => typeAndBody.EventType != null)
                             .Select(typeAndBody =>
                                     JsonConvert.DeserializeObject(typeAndBody.Body, typeAndBody.EventType, Settings))
                             .Cast<IEvent>();
        }

        private struct StoredEvent
        {
            public string StreamName;
            public string EventTypeName;
            public string Body;
            public Guid AggregateId;
            public long SequenceNumber;
            public DateTimeOffset Timestamp;
            public string ETag { get; set; }
        }
    }
}
