// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    /// <summary>
    /// Performs a migration using a SQL script stored as an embedded resource.
    /// </summary>
    internal class ScriptBasedDbMigrator : IDbMigrator
    {
        // resourceNameParsesr parses out the scope and version from the resource name
        // e.g. Microsoft.Its.Domain.Sql.EventStoreDbContext-1_0_0_192.sql
        //                               ^-----------------^ ^-------^
        //                                      scope         version
        private static readonly Regex resourceNameParser = new Regex(@"(?<scope>[\w]+)\-(?<version>[0-9_]+)",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ScriptBasedDbMigrator(string resourceName)
        {
            if (resourceName == null)
            {
                throw new ArgumentNullException("resourceName");
            }

            var matches = resourceNameParser.Match(resourceName);

            var version = matches.Groups["version"]
                .Value
                .Replace("_", ".");

            MigrationVersion = new Version(version);

            MigrationScope = matches.Groups["scope"].Value;

            if (MigrationScope == null)
            {
                throw new ArgumentException("resourceName was not in the expected format");
            }

            var stream = typeof (ScriptBasedDbMigrator).Assembly
                                                       .GetManifestResourceStream(resourceName);

            SqlText = new StreamReader(stream).ReadToEnd();
        }

        public ScriptBasedDbMigrator(string sqlText, Version migrationVersion, string scope)
        {
            if (sqlText == null)
            {
                throw new ArgumentNullException("sqlText");
            }
            if (migrationVersion == null)
            {
                throw new ArgumentNullException("migrationVersion");
            }
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
            SqlText = sqlText;
            MigrationVersion = migrationVersion;
        }

        public string SqlText { get; private set; }

        public string MigrationScope { get; private set; }

        public Version MigrationVersion { get; private set; }

        public MigrationResult Migrate(IDbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = SqlText;
                command.CommandTimeout = 600;
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