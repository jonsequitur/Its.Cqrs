// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A scheduled command that is stored in the event store.
    /// </summary>
    public interface IScheduledCommandEvent : IEvent, IScheduledCommand
    {
    }
}