// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public class CustomerAccountSnapshot : ISnapshot
    {
        public string UserName { get; set; }
        public EmailAddress EmailAddress { get; set; }
        public bool NoSpam { get; set; }
        public EmailSubject[] CommunicationsSent { get; set; }
        public Guid AggregateId { get; set; }
        public long Version { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
        public string AggregateTypeName { get; set; }
        public string[] ETags { get; set; }
    }
}
