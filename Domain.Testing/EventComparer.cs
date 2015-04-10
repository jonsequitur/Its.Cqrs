// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Its.Domain.Testing
{
    public class EventComparer : IEqualityComparer<IStoredEvent>
    {
        public bool Equals(IStoredEvent x, IStoredEvent y)
        {
            return x.AggregateId == y.AggregateId &&
                   x.SequenceNumber == y.SequenceNumber;
        }

        public int GetHashCode(IStoredEvent obj)
        {
            return (obj.AggregateId + "|" + obj).GetHashCode();
        }
    }
}
