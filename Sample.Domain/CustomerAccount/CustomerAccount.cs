// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class CustomerAccount : EventSourcedAggregate<CustomerAccount>
    {
        private readonly HashSet<EmailSubject> communicationsSent = new HashSet<EmailSubject>();

        public CustomerAccount(
            Guid id,
            IEnumerable<IEvent> eventHistory) :
                base(id, eventHistory)
        {
        }

        public CustomerAccount(Guid? id = null) : base(id)
        {
        }

        public CustomerAccount(
            ISnapshot snapshot,
            IEnumerable<IEvent> additionalEvents = null) : base(snapshot, additionalEvents)
        {
        }

        public string UserName { get; private set; }

        public EmailAddress EmailAddress { get; private set; }

        public bool NoSpam { get; private set; }

        public HashSet<EmailSubject> CommunicationsSent => communicationsSent;
    }
}