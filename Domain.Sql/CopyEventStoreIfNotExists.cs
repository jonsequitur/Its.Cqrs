// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.SqlClient;

namespace Microsoft.Its.Domain.Sql
{
    internal class CopyEventStoreIfNotExists : CreateDatabaseIfNotExists<EventStoreDbContext>
    {
        private readonly string seedFromConnectionString;

        // TODO: (CopyEventStoreIfNotExists) test
        // TODO: (CopyEventStoreIfNotExists) support for event transform (e.g. cleansing / filtering / etc) during copy... should this just be a projector + catchup? 

        public CopyEventStoreIfNotExists(string seedFromConnectionString)
        {
            if (seedFromConnectionString == null)
            {
                throw new ArgumentNullException("seedFromConnectionString");
            }
            this.seedFromConnectionString = seedFromConnectionString;
        }

        protected override void Seed(EventStoreDbContext context)
        {
            base.Seed(context);

            using (var seedStore = new EventStoreDbContext(seedFromConnectionString))
            {
                using (var command = seedStore.Database.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM EventStore.Events";

                    var seedConnection = (SqlConnection) seedStore.Database.Connection;
                    seedConnection.Open();

                    using (var reader = command.ExecuteReader())
                    using (var connection = (SqlConnection) context.Database.Connection)
                    {
                        connection.Open();
                        using (var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, null)
                        {
                            BatchSize = 1000,
                            DestinationTableName = "EventStore.Events",
                            EnableStreaming = false
                        })
                        {
                            bulk.WriteToServer(reader);
                        }
                    }
                }
            }
        }
    }
}