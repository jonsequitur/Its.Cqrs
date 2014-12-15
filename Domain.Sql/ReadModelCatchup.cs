// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Updates read models using <see cref="ReadModelDbContext" /> based on events after they have been added to an event store.
    /// </summary>
    public class ReadModelCatchup : ReadModelCatchup<ReadModelDbContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadModelCatchup"/> class.
        /// </summary>
        /// <param name="projectors">The projectors to be updated as new events are added to the event store.</param>
        public ReadModelCatchup(params object[] projectors) : base(projectors)
        {
        }
    }
}