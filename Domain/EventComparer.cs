// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    internal class EventComparer : IEqualityComparer<IEvent>
    {
        public static readonly EventComparer Instance = new EventComparer(); 

        public bool Equals(IEvent x, IEvent y)
        {
            return x.AggregateId == y.AggregateId &&
                   x.SequenceNumber == y.SequenceNumber;
        }

        public int GetHashCode(IEvent obj)
        {
            unchecked
            {
                return (obj.SequenceNumber.GetHashCode() * 397) ^ obj.AggregateId.GetHashCode();
            }
        }
    }
}