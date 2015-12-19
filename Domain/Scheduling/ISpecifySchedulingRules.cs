// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     Defines rules for how scheduling of the command should be handled.
    /// </summary>
    public interface ISpecifySchedulingRules
    {
        /// <summary>
        ///     Determines whether the command can be delivered during a call to <see cref="ICommandScheduler{T}.Schedule" />.
        /// </summary>
        bool CanBeDeliveredDuringSchedule { get; }

        /// <summary>
        ///     Determines whether the command must be stored durably during a call to <see cref="ICommandScheduler{T}.Schedule" />
        ///     .
        /// </summary>
        bool RequiresDurableScheduling { get; }
    }
}