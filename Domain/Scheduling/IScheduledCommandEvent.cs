// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A scheduled command that is stored in the event store.
    /// </summary>
    [Obsolete("This interface is for backwards compatibility and will be removed in a future version.")]
    public interface IScheduledCommandEvent : IEvent, IScheduledCommand
    {
    }
}