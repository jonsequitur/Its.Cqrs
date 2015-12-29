// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class CommandExtensions
    {
        /// <summary>
        ///     Determines whether the command can be delivered during a call to <see cref="ICommandScheduler{T}.Schedule" />.
        /// </summary>
        /// <param name="command">The command.</param>
        public static bool CanBeDeliveredDuringScheduling(this ICommand command)
        {
            return command.IfTypeIs<ISpecifySchedulingBehavior>()
                          .Then(c => c.CanBeDeliveredDuringScheduling)
                          .Else(() => true);
        }

        /// <summary>
        ///     Determines whether the command must be stored durably during a call to <see cref="ICommandScheduler{T}.Schedule" />
        ///     .
        /// </summary>
        /// <param name="command">The command.</param>
        public static bool RequiresDurableScheduling(this ICommand command)
        {
            return command.IfTypeIs<ISpecifySchedulingBehavior>()
                          .Then(c => c.RequiresDurableScheduling)
                          .Else(() => true);
        }
    }
}