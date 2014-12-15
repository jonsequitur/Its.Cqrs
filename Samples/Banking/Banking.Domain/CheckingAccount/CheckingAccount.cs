// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain;

namespace Sample.Banking.Domain
{
    public partial class CheckingAccount : EventSourcedAggregate<CheckingAccount>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckingAccount"/> class.
        /// </summary>
        /// <param name="eventHistory">The event history.</param>
        /// <remarks>This constructor is used when sourcing the aggregate from its event history.</remarks>
        public CheckingAccount(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckingAccount" /> class.
        /// </summary>
        /// <param name="checkingAccountId">The checking account identifier.</param>
        /// <param name="customerAccountId">The customer account id for the customer account that owns the checking account.</param>
        public CheckingAccount(Guid checkingAccountId, Guid customerAccountId) : base(checkingAccountId)
        {
            RecordEvent(new Opened
            {
                AggregateId = checkingAccountId,
                CustomerAccountId = customerAccountId
            });
        }

        public decimal Balance { get; private set; }

        public DateTimeOffset DateOpened { get; private set; }

        public DateTimeOffset? DateClosed { get; private set; }

        public bool IsOverdrawn { get; private set; }
    }
}