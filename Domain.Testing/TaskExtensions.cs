// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    public static class TaskExtensions
    {
        public static async Task TimeoutAfter(
            this Task task,
            TimeSpan timespan)
        {
            if (task.IsCompleted)
            {
                return;
            }

            if (task == await Task.WhenAny(task, Task.Delay(timespan)))
            {
                await task;
            }
            else
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// Provides a way to specify the intention to fire and forget a task and suppress the compiler warning that results from unawaited tasks.
        /// </summary>
        internal static void DontAwait(this Task task)
        {
        }
    }
}