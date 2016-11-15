// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A command that has been scheduled for future execution.
    /// </summary>
    public interface IScheduledCommand<in TAggregate> :
        IScheduledCommand
    {
        /// <summary>
        /// Gets the command to be applied at a later time.
        /// </summary>
        ICommand<TAggregate> Command { get; }

        /// <summary>
        /// Gets the id of the target object to which the command will be applied when delivered.
        /// </summary>
        string TargetId { get; }
    }
}