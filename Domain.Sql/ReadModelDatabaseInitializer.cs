// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Initializes a read model database with a single catchup run if the database does not exist or its schema has changed.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the db context.</typeparam>
    public class ReadModelDatabaseInitializer<TDbContext> : CreateAndMigrate<TDbContext>
        where TDbContext : ReadModelDbContext, new()
    {
        protected override bool DropDatabaseIfModelIsIncompatible
        {
            get
            {
                return true;
            }
        }
    }
}