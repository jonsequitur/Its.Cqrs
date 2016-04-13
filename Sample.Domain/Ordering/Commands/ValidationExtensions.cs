// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Its.Validation;
using Its.Validation.Configuration;

namespace Test.Domain.Ordering
{
    public static class ValidationExtensions
    {
        public sealed class ActionIsRetryable
        {
            private ActionIsRetryable()
            {
            }

            public static ActionIsRetryable Instance = new ActionIsRetryable();
        }

        public static ValidationRule<T> Retryable<T>(this ValidationRule<T> o)
        {
            return o.With(ActionIsRetryable.Instance);
        }

        public static bool IsRetryable(this FailedEvaluation evaluation)
        {
            return evaluation.Result<ActionIsRetryable>() != null;
        }
    }
}
