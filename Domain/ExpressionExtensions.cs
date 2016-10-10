// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides methods for working with expressions.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Gets member name specified by the expression.
        /// </summary>
        public static string MemberName<T>(this Expression<Func<T, object>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var memberExpression = expression.Body as MemberExpression;

            if (memberExpression != null)
            {
                return memberExpression.Member.Name;
            }

            // when the return type of the expression is a value type, it contains a call to Convert, resulting in boxing, so we get a UnaryExpression instead
            var unaryExpression = expression.Body as UnaryExpression;
            if (unaryExpression != null)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
                if (memberExpression != null)
                {
                    return memberExpression.Member.Name;
                }
            }

            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression != null)
            {
                return methodCallExpression.Method.Name;
            }

            return string.Empty;
        }
    }
}
