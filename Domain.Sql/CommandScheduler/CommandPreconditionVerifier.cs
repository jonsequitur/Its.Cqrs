// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class CommandPreconditionVerifier : ICommandPreconditionVerifier
    {
        private readonly Func<EventStoreDbContext> createEventStoreDbContext;

        public CommandPreconditionVerifier(Func<EventStoreDbContext> createEventStoreDbContext)
        {
            if (createEventStoreDbContext == null)
            {
                throw new ArgumentNullException("createEventStoreDbContext");
            }
            this.createEventStoreDbContext = createEventStoreDbContext;
        }

        public async Task<bool> VerifyPrecondition(IScheduledCommand scheduledCommand)
        {
            if (scheduledCommand == null)
            {
                throw new ArgumentNullException("scheduledCommand");
            }

            if (scheduledCommand.DeliveryPrecondition == null)
            {
                return true;
            }

            using (var eventStore = createEventStoreDbContext())
            {
                return await eventStore.Events.AnyAsync(
                    e => e.AggregateId == scheduledCommand.DeliveryPrecondition.AggregateId &&
                         e.ETag == scheduledCommand.DeliveryPrecondition.ETag);
            }
        }
    }
}