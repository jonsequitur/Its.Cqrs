// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.Its.Domain.Sql.Migrations;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Creates and migrates an event store database.
    /// </summary>
    public class EventStoreDatabaseInitializer<TContext> :
        CreateAndMigrate<TContext>
        where TContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDatabaseInitializer{TContext}"/> class.
        /// </summary>
        public EventStoreDatabaseInitializer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDatabaseInitializer{TContext}"/> class.
        /// </summary>
        /// <param name="migrators">The migrations to apply during initialization.</param>
        public EventStoreDatabaseInitializer(params IDbMigrator[] migrators) : base(migrators)
        {
        }

        /// <summary>
        /// Determines whether the database should be rebuilt.
        /// </summary>
        protected override bool ShouldRebuildDatabase(
            TContext context, 
            Version latestVersion) => false;
    }
}