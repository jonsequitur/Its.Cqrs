// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Entity;
using Microsoft.Its.Domain.Sql.Migrations;

namespace Microsoft.Its.Domain.Sql
{
    internal class SetDatabaseVersion<TContext> : IDbMigrator where TContext : DbContext
    {
        public SetDatabaseVersion(Version version = null)
        {
            MigrationVersion = version ?? typeof (TContext).Assembly.GetName().Version;
        }

        public string MigrationScope => "Version";

        public Version MigrationVersion { get; }

        public MigrationResult Migrate(DbContext context)
        {
            return new MigrationResult
            {
                Log = $"Version {MigrationVersion} initialized at {DateTimeOffset.Now}",
                MigrationWasApplied = true
            };
        }
    }
}