// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class ReadModels2DbContext : ReadModelDbContext
    {
        static ReadModels2DbContext()
        {
            Database.SetInitializer(new ReadModelDatabaseInitializer<ReadModels2DbContext>());
        }

        public ReadModels2DbContext()
            : base(@"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsReadModels2DbContext")
        {
        }
    }
}