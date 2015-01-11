// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Web.Http.ModelBinding;
using Its.Validation;

namespace Microsoft.Its.Domain.Api
{
    internal static class ValidationExtensions
    {
        public static void AddValidationFailures(this ModelStateDictionary modelState, IEnumerable<FailedEvaluation> failures)
        {
            if (failures == null)
            {
                return;
            }

            foreach (var failure in failures
                .Where(f => !string.IsNullOrWhiteSpace(f.Message) && !f.Message.Contains("{")))
            {
                modelState.AddModelError(failure.MemberPath, failure.Message);
            }
        }
    }
}
