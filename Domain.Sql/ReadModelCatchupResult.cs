// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql
{
    public enum ReadModelCatchupResult
    {
        CatchupAlreadyInProgress = 0,
        CatchupRanButNoNewEvents = 1,
        CatchupRanAndHandledNewEvents = 2
    }
}
