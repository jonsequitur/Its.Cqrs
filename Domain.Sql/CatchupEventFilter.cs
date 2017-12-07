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
        private static readonly Lazy<MethodInfo> containsMethod = new Lazy<MethodInfo>(ContainsMethod);

        public CatchupEventFilter(IEnumerable<MatchEvent> eventCriteria)
        {
            if (eventCriteria == null)
            {
                throw new ArgumentNullException(nameof(eventCriteria));
            }

            Filter = FilterForEventMatchCustomBuild(eventCriteria);
        }

        public Expression<Func<StorableEvent, bool>> Filter { get; }

        private static MethodInfo ContainsMethod()
        {
            var method = typeof(Enumerable).GetRuntimeMethods().Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);
            return method.MakeGenericMethod(typeof(string));
        }

        private static Expression<Func<StorableEvent, bool>> FilterForEventMatchCustomBuild(IEnumerable<MatchEvent> criterias)
        {
            var seParam = Expression.Parameter(typeof(StorableEvent), "storableEvent");
            var expressionPropertyStreamName = Expression.Property(seParam, "StreamName");
            var expressionPropertyType = Expression.Property(seParam, "Type");

            var groupByStreamName = criterias.GroupBy(c => c.StreamName, c => c.Type);

            var body = groupByStreamName
                .Select(c => GetExpression(c, expressionPropertyStreamName, expressionPropertyType))
                .Aggregate(Expression.OrElse);

            return Expression.Lambda<Func<StorableEvent, bool>>(body, seParam);
        }

        private static BinaryExpression GetExpression(IGrouping<string, string> criteria, MemberExpression expressionPropertyStreamName, MemberExpression expressionPropertyType)
        {
            var streamNameMatches = Expression.Equal(expressionPropertyStreamName, Expression.Constant(criteria.Key));
            var typeMatches = Expression.Call(containsMethod.Value, Expression.Constant(criteria.ToArray()), expressionPropertyType);
            return Expression.AndAlso(streamNameMatches, typeMatches);
        }
    }
}