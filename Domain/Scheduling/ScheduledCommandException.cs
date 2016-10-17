// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Thrown when a scheduled command fails on delivery due to a concurrency, validation, or authorization exception.
    /// </summary>
    [DebuggerStepThrough]
    public class ScheduledCommandException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCommandException"/> class.
        /// </summary>
        /// <param name="failure">The failure.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public ScheduledCommandException(CommandFailed failure)
            : base(failure.Exception
                          .IfNotNull()
                          .Then(e => e.Message)
                          .Else(() => "Scheduled command failed"),
                failure.Exception)
        {
            if (failure == null)
            {
                throw new ArgumentNullException(nameof(failure));
            }
            Failure = failure;
        }

        /// <summary>
        /// Gets the failure that caused the exception.
        /// </summary>
        public CommandFailed Failure { get; private set; }
    }
}