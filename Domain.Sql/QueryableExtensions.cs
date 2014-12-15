// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
        public static IEnumerable<StorableEvent> RelatedEvents(
            this IQueryable<StorableEvent> events,
            params Guid[] relatedToAggregateIds)
        {
            var ids = new HashSet<Guid>(relatedToAggregateIds);

            var relatedEvents = new HashSet<StorableEvent>();

            int currentCount;

            do
            {
                currentCount = relatedEvents.Count;

                var unqueriedIds = ids.Where(id => ! relatedEvents.Select(e => e.AggregateId).Contains(id));

                var newEvents = events.Where(e => unqueriedIds.Any(id => id == e.AggregateId)).ToArray();

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

        private static IEnumerable<Guid> ExtractGuids(this string eventBody)
        {
            return guidRegex.Matches(eventBody)
                            .Cast<Match>()
                            .Select(match => Guid.Parse(match.Groups[1].ToString()));
        }
    }
}