// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    /// <summary>
    /// Performs a one-time database migration or other other operation.
    /// </summary>
    public interface IDbMigrator
    {
        /// <summary>
        /// Gets the migration version.
        /// </summary>
        Version MigrationVersion { get; }

        /// <summary>
        /// Migrates a database using the specified connection.
        /// </summary>
        void Migrate(IDbConnection connection);
    }
}