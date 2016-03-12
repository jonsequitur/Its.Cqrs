// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Sql.Migrations;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Initializes a read model database with a single catchup run if the database does not exist or its schema has changed.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the db context.</typeparam>
    public class ReadModelDatabaseInitializer<TDbContext> : CreateAndMigrate<TDbContext>
        where TDbContext : ReadModelDbContext, new()
    {
        private readonly SetDatabaseVersion<TDbContext> version;

        public ReadModelDatabaseInitializer() : this(new SetDatabaseVersion<TDbContext>())
        {
        }

        public ReadModelDatabaseInitializer(Version version) : this(new SetDatabaseVersion<TDbContext>(version))
        {
        }

        internal ReadModelDatabaseInitializer(SetDatabaseVersion<TDbContext> version) :
            base(new IDbMigrator[] { version })
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }
            this.version = version;
        }

        /// <summary>
        /// Determines whether the database should be rebuilt.
        /// </summary>
        protected override bool ShouldRebuildDatabase(
            TDbContext context,
            Version latestVersion)
        {
             if (latestVersion < version.MigrationVersion)
             {
                return true;
             }

            if (!context.Database.CompatibleWithModel(false))
            {
                return true;
            }

            return false;
        }
    }
}