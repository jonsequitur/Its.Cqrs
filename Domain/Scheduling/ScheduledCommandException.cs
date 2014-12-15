// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public class ScheduledCommandException : Exception
    {
        public ScheduledCommandException(ScheduledCommandFailure failure)
            : base(failure.Exception
                          .IfNotNull()
                          .Then(e => e.Message)
                          .Else(() => "Scheduled command failed"),
                   failure.Exception)
        {
            if (failure == null)
            {
                throw new ArgumentNullException("failure");
            }
            Failure = failure;
        }

        public ScheduledCommandFailure Failure { get; private set; }
    }
}