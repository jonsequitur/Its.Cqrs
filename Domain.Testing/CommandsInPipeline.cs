// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// A collection of the commands currently scheduled and not yet delivered.
    /// </summary>
    internal class CommandsInPipeline : IEnumerable<IScheduledCommand>
    {
        private readonly ConcurrentDictionary<IScheduledCommand, DateTimeOffset> commands = new ConcurrentDictionary<IScheduledCommand, DateTimeOffset>();

        public void Add(IScheduledCommand command)
        {
            var now = Clock.Now();
            commands.AddOrUpdate(
                command,
                now,
                (c, t) => now);
        }

        public void Remove(IScheduledCommand command)
        {
            DateTimeOffset _;
            commands.TryRemove(command, out _);
        }

        public async Task Done()
        {
            while (true)
            {
                var now = Clock.Current;
                if (!commands.Keys.Any(c => c.IsDue(now)))
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(5));
            }
        }

        public IEnumerator<IScheduledCommand> GetEnumerator() => commands.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}