// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides methods for various types of event store queries. 
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Retrieves all events across all aggregates that are related to the specified aggregate ids, in the order in which they were recorded.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="relatedToAggregateIds">The aggregate ids to which the events relate.</param>
        public static async Task<IEnumerable<StorableEvent>> RelatedEvents(
            this IQueryable<StorableEvent> events,
            params Guid[] relatedToAggregateIds)
        {
            var ids = new HashSet<Guid>(relatedToAggregateIds);

            var relatedEvents = new HashSet<StorableEvent>();

            int currentCount;

            do
            {
                currentCount = relatedEvents.Count;

                var unqueriedIds = ids.Where(id => !relatedEvents.Select(e => e.AggregateId).Contains(id));

                var newEvents = await events.Where(e => unqueriedIds.Any(id => id == e.AggregateId)).ToArrayAsync();

                relatedEvents.UnionWith(newEvents);

                var moreIds = newEvents
                    .SelectMany(e => e.Body.ExtractGuids())
                    .Distinct()
                    .ToArray();

                if (!moreIds.Any())
                {
                    break;
                }

                ids.UnionWith(moreIds);
            } while (currentCount != relatedEvents.Count);

            return relatedEvents.OrderBy(e => e.Id);
        }

        private static readonly Regex guidRegex = new Regex(
            "\"([a-fA-F0-9-\\{\\}]{36})\"", RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        private static IEnumerable<Guid> ExtractGuids(this string eventBody) =>
            guidRegex.Matches(eventBody)
                     .Cast<Match>()
                     .Select(match => Guid.Parse(match.Groups[1].ToString()));

        internal static IQueryable<StorableEvent> Where(
            this IQueryable<StorableEvent> eventQuery,
            MatchEvent[] matchEvents,
            Expression<Func<StorableEvent, bool>> filter)
        {
            matchEvents = matchEvents ?? new[] { new MatchEvent() };

            // if specific event types are requested, we can optimize the event store query
            // if Event or IEvent are requested, we don't filter -- this requires reading every event
            if (matchEvents.Any())
            {
                var eventTypes = matchEvents.Select(m => m.Type).Distinct().ToArray();
                var aggregates = matchEvents.Select(m => m.StreamName).Distinct().ToArray();

                if (!aggregates.Any(streamName => string.IsNullOrWhiteSpace(streamName) || streamName == MatchEvent.Wildcard))
                {
                    if (!eventTypes.Any(type => string.IsNullOrWhiteSpace(type) || type == MatchEvent.Wildcard))
                    {
                        // Filter on StreamName and Type
                        var projectionEventFilter = new CatchupEventFilter(matchEvents);
                        eventQuery = eventQuery.Where(projectionEventFilter.Filter);
                    }
                    else
                    {
                        // Filter on StreamName
                        eventQuery = eventQuery.Where(e => aggregates.Contains(e.StreamName));
                    }
                }
            }

            if (filter != null)
            {
                eventQuery = eventQuery.Where(filter);
            }

            return eventQuery;
        }
    }
}