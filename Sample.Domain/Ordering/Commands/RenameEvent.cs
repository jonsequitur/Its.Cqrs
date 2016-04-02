// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;
using Test.Domain.Ordering;

namespace Test.Domain.Ordering
{
    public class RenameEvent : Command<Order>
    {
        public readonly long sequenceNumber;
        public readonly string newName;

        public RenameEvent(long sequenceNumber, string newName)
        {
            this.sequenceNumber = sequenceNumber;
            this.newName = newName;
        }
    }
}