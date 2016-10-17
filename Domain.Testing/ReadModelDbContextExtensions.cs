// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using Microsoft.Its.Domain.Sql;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Provides methods for working with the read model database context.
    /// </summary>
    public static class ReadModelDbContextExtensions
    {
        /// <summary>
        /// Sets the read model info cursors to the specified event id.
        /// </summary>
        /// <param name="db">The read model database.</param>
        /// <param name="toEventId">To event id to which to set the read model cursors.</param>
        /// <param name="addForProjectors">Specifies projectors to add read model info for, in case their read models have not previously been built.</param>
        [Obsolete]
        public static void SetReadModelTrackingTo(this DbContext db,
                                                  long toEventId, 
                                                  params object[] addForProjectors)
        {
            // forward all existing read model tracking
            foreach (var readModelInfo in db.Set<ReadModelInfo>())
            {
                readModelInfo.CurrentAsOfEventId = toEventId;
            }

            foreach (var projector in addForProjectors)
            {
                var projectorName = ReadModelInfo.NameForProjector(projector);

                db.Set<ReadModelInfo>().AddOrUpdate(
                    i => i.Name,
                    new ReadModelInfo
                    {
                        Name = projectorName,
                        CurrentAsOfEventId = toEventId
                    });
            }

            db.SaveChanges();
        }
    }
}
