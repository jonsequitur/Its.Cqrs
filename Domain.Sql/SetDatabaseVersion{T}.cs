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
        private readonly Version version;

        public SetDatabaseVersion(Version version = null)
        {
            this.version = version ?? typeof (TContext).Assembly.GetName().Version;
        }

        public string MigrationScope
        {
            get
            {
                return "Version";
            }
        }

        public Version MigrationVersion
        {
            get
            {
                return version;
            }
        }

        public MigrationResult Migrate(IDbConnection connection)
        {
            return new MigrationResult
            {
                Log = string.Format("Version {0} initialized at {1}", MigrationVersion, DateTimeOffset.Now),
                MigrationWasApplied = true
            };
        }
    }
}