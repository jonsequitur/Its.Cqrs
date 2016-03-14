// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Domain.Sql.Migrations;
using Microsoft.Its.Recipes;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadModelDatabaseInitializer{TDbContext}"/> class.
        /// </summary>
        public ReadModelDatabaseInitializer() : this(new SetDatabaseVersion<TDbContext>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadModelDatabaseInitializer{TDbContext}"/> class.
        /// </summary>
        /// <param name="version">The database version for the current code. If this value is higher than the version found in the database, the database will be dropped and rebuilt.</param>
        public ReadModelDatabaseInitializer(Version version) : this(new SetDatabaseVersion<TDbContext>(version))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDatabaseInitializer{TContext}"/> class.
        /// </summary>
        /// <param name="migrators">The migrations to apply during initialization.</param>
        /// <param name="version">The database version for the current code. If this value is higher than the version found in the database, the database will be dropped and rebuilt.</param>
        public ReadModelDatabaseInitializer(Version version, IDbMigrator[] migrators) : this(new SetDatabaseVersion<TDbContext>(version), migrators)
        {
        }

        internal ReadModelDatabaseInitializer(SetDatabaseVersion<TDbContext> version, IDbMigrator[] migrators = null) :
            base(migrators.OrEmpty()
                          .Concat(new[] { version })
                          .ToArray())
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