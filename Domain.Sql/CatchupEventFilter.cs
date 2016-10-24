// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain.Sql
{
    internal class CatchupEventFilter
    {
        public CatchupEventFilter(IEnumerable<MatchEvent> eventCriteria)
        {
            if (eventCriteria == null)
            {
                throw new ArgumentNullException(nameof(eventCriteria));
            }

            Filter = FilterForEventMatchCustomBuild(eventCriteria);
        }

        public Expression<Func<StorableEvent, bool>> Filter { get; }

        private static BinaryExpression GetExpression(MatchEvent criteria, ParameterExpression p)
        {
            var streamNameMatches = Expression.Equal(
                Expression.Property(p, "StreamName"),
                Expression.Constant(criteria.StreamName));
            var typeMatches = Expression.Equal(
                Expression.Property(p, "Type"),
                Expression.Constant(criteria.Type));
            return Expression.AndAlso(streamNameMatches, typeMatches);
        }

        private static Expression<Func<StorableEvent, bool>> FilterForEventMatchCustomBuild(IEnumerable<MatchEvent> criteria)
        {
            var seParam = Expression.Parameter(typeof(StorableEvent), "storableEvent");

            var body = criteria.Select(c => GetExpression(c, seParam))
                               .Aggregate(Expression.OrElse);

            return Expression.Lambda<Func<StorableEvent, bool>>(body, seParam);
        }
    }
}