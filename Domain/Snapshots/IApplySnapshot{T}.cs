// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    internal interface IApplySnapshot<T> 
        where T : class, IEventSourced
    {
        void ApplySnapshot(ISnapshot snapshot, T aggregate);
    }
}