// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    public interface ICreateSnapshot<in TAggregate>
        where TAggregate : class, IEventSourced
    {
        ISnapshot CreateSnapshot(TAggregate aggregate);
    }
}