// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class ScheduledCommand : IScheduledCommand
    {
        public Guid AggregateId { get; set; }

        public long SequenceNumber { get; set; }

        public string AggregateType { get; set; }

        public DateTimeOffset CreatedTime { get; set; }

        public DateTimeOffset? DueTime { get; set; }

        public DateTimeOffset? AppliedTime { get; set; }

        public DateTimeOffset? FinalAttemptTime { get; set; }

        public string SerializedCommand { get; set; }

        public int Attempts { get; set; }

        public Clock Clock { get; set; }

        public ScheduledCommandResult Result { get; set; }

        internal bool NonDurable { get; set; }

        ScheduledCommandPrecondition IScheduledCommand.DeliveryPrecondition
        {
            get
            {
                return SerializedCommand.FromJsonTo<JObject>()
                                        .IfHas(d => d.DeliveryPrecondition)
                                        .ElseDefault();
            }
        }

        DateTimeOffset IEvent.Timestamp
        {
            get
            {
                return CreatedTime;
            }
        }

        string IEvent.ETag
        {
            get
            {
                return null;
            }
        }
    }
}
