// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public partial class CustomerAccount : EventSourcedAggregate<CustomerAccount>
    {
        private readonly HashSet<EmailSubject> communicationsSent = new HashSet<EmailSubject>();

        public CustomerAccount(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
        {
        }

        public CustomerAccount(Guid? id = null) : base(id)
        {
        }

        protected string UserName { get; private set; }

        public EmailAddress EmailAddress { get; private set; }

        public bool NoSpam { get; private set; }

        public HashSet<EmailSubject> CommunicationsSent
        {
            get
            {
                return communicationsSent;
            }
        }
    }
}