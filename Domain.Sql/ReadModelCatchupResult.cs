// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Gets the result of a single run of a read model catchup.
    /// </summary>
    public enum ReadModelCatchupResult
    {
        /// <summary>
        /// Another catchup operation was already in progress.
        /// </summary>
        CatchupAlreadyInProgress = 0,

        /// <summary>
        /// The catchup ran but no new events were found.
        /// </summary>
        CatchupRanButNoNewEvents = 1,

        /// <summary>
        /// The catchup ran and handled new events.
        /// </summary>
        CatchupRanAndHandledNewEvents = 2
    }
}