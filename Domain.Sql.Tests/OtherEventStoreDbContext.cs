// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class OtherEventStoreDbContext : EventStoreDbContext
    {
        static OtherEventStoreDbContext()
        {
            Database.SetInitializer(new EventStoreDatabaseInitializer<OtherEventStoreDbContext>());
        }

        public OtherEventStoreDbContext()
            : base(@"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsCatchupTestsOtherEventStore")
        {
        }
    }
}