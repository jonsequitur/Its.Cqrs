// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain;

namespace Sample.Banking.Domain
{
    public partial class CustomerAccount : EventSourcedAggregate<CustomerAccount>
    {
        /// <summary>
        /// Initializes a new instance of the CustomerAccount class.
        /// </summary>
        /// <param name="id">The aggregate's unique id.</param><param name="eventHistory">The event history.</param>
        public CustomerAccount(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Its.Domain.EventSourcedAggregate`1"/> class by applying the specified command.
        /// </summary>
        /// <param name="createCustomerAccount">The create command.</param>
        public CustomerAccount(ConstructorCommand<CustomerAccount> createCustomerAccount) : base(createCustomerAccount)
        {
        }

        internal ISet<Guid> CheckingAccounts = new HashSet<Guid>();
    }
}