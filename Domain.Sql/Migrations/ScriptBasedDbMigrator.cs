// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.IO;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    /// <summary>
    /// Performs a migration using a SQL script stored as an embedded resource.
    /// </summary>
    internal class ScriptBasedDbMigrator : IDbMigrator
    {
        public ScriptBasedDbMigrator(string resourceName)
        {
            MigrationVersion = resourceName.Split('.', '-')
                                           .Where(s => s.Contains("_"))
                                           .Select(s => s.Replace("_", "."))
                                           .Select(s => new Version(s))
                                           .Single();

            var stream = typeof (ScriptBasedDbMigrator).Assembly
                                                       .GetManifestResourceStream(resourceName);

            SqlText = new StreamReader(stream).ReadToEnd();
        }

        public ScriptBasedDbMigrator(string sqlText, Version migrationVersion)
        {
            SqlText = sqlText;
            MigrationVersion = migrationVersion;
        }

        public string SqlText { get; private set; }

        public Version MigrationVersion { get; private set; }

        public MigrationResult Migrate(IDbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = SqlText;
                command.ExecuteNonQuery();
            }

            return new MigrationResult
            {
                MigrationWasApplied = true, 
                Log = SqlText
            };
        }
    }
}