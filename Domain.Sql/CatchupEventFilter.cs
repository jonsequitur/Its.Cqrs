// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;


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

        private static Expression<Func<StorableEvent, bool>> FilterForEventMatchCustomBuild(IEnumerable<MatchEvent> criterias)
        {
            var seParam = Expression.Parameter(typeof(StorableEvent), "storableEvent");
            var groupByStreamName = criterias.GroupBy(c => c.StreamName, c => c.Type);

            var body = groupByStreamName
                .Select(c => GetExpression(c, seParam))
                .Aggregate(Expression.OrElse);

            return Expression.Lambda<Func<StorableEvent, bool>>(body, seParam);
        }

        private static BinaryExpression GetExpression(IGrouping<string, string> criteria, ParameterExpression p)
        {
            var streamNameMatches = Expression.Equal(Expression.Property(p, "StreamName"), Expression.Constant(criteria.Key));

            var method = typeof(Enumerable).GetRuntimeMethods().Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);
            var containsMethod = method.MakeGenericMethod(typeof(string));
            var typeMatches = Expression.Call(containsMethod, Expression.Constant(criteria.ToArray()), Expression.Property(p, "Type"));
            return Expression.AndAlso(streamNameMatches, typeMatches);
        }
    }
}