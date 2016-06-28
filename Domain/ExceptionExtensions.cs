// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Its.Domain
{
    internal static class ExceptionExtensions
    {
        private static readonly Type[] uninterestingExceptionTypes = 
        {
            typeof (AggregateException),
            typeof (TargetInvocationException)
        };

        public static Exception FindInterestingException(this Exception exception)
        {
            while (uninterestingExceptionTypes.Contains(exception.GetType()) &&
                   exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            return exception;
        }
    }
}