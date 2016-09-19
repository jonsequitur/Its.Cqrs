// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public ScriptBasedDbMigrator(string resourceName, Assembly assembly)
        {
            if (resourceName == null)
            {
                throw new ArgumentNullException(nameof(resourceName));
            }
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
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

            var stream = assembly.GetManifestResourceStream(resourceName);

            SqlText = new StreamReader(stream).ReadToEnd();
        }

        public ScriptBasedDbMigrator(string sqlText, Version migrationVersion, string scope)
        {
            if (sqlText == null)
            {
                throw new ArgumentNullException(nameof(sqlText));
            }
            if (migrationVersion == null)
            {
                throw new ArgumentNullException(nameof(migrationVersion));
            }
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }
            SqlText = sqlText;
            MigrationVersion = migrationVersion;
            MigrationScope = scope;
        }

        public string SqlText { get; }

        public string MigrationScope { get; }

        public Version MigrationVersion { get; }

        public MigrationResult Migrate(DbContext context)
        {
            var originalTimeout = context.Database.CommandTimeout;

            try
            {
                context.Database.CommandTimeout = 600;
                context.Database.ExecuteSqlCommand(
                    TransactionalBehavior.EnsureTransaction,
                    SqlText);
            }
            finally
            {
                context.Database.CommandTimeout = originalTimeout;
            }

            return new MigrationResult
            {
                MigrationWasApplied = true,
                Log = SqlText
            };
        }
    }
}