// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace Microsoft.Its.Domain.Sql
{
    public class EventStoreDatabaseInitializer<TContext> :
        CreateAndMigrate<TContext>
        where TContext : DbContext
    {
    }

    internal static class DbCommandExtensions
    {
        public static IEnumerable<IEnumerable<dynamic>> QueryDynamic(
            this DbConnection connection,
            string sql,
            IDictionary<string, object> parameters = null)
        {
            using (var command = connection.PrepareCommand(sql, parameters))
            {
                return command.ExecuteQueriesToDynamic();
            }
        }

        public static void Execute(
            this DbConnection connection,
            string sql,
            IDictionary<string, object> parameters = null)
        {
            using (var command = connection.PrepareCommand(sql, parameters))
            {
                command.ExecuteNonQuery();
            }
        }

        public static DbCommand PrepareCommand(
            this DbConnection connection,
            string sql,
            IDictionary<string, object> parameters = null)
        {
            var command = connection.CreateCommand();
            {
                command.CommandType = CommandType.Text;
                command.CommandText = sql;

                if (parameters != null)
                {
                    foreach (var pair in parameters)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = pair.Key;
                        parameter.Value = pair.Value ?? DBNull.Value;
                        command.Parameters.Add(parameter);
                    }
                }

                return command;
            }
        }

        public static IEnumerable<IEnumerable<dynamic>> ExecuteQueriesToDynamic(
            this IDbCommand command)
        {
            using (var reader = command.ExecuteReader())
            {
                do
                {
                    var values = new object[reader.FieldCount];
                    var names = Enumerable.Range(0, reader.FieldCount)
                                          .Select(reader.GetName)
                                          .ToArray();

                    var currentResult = new List<ExpandoObject>();

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        var expando = new ExpandoObject();
                        for (var i = 0; i < values.Length; i++)
                        {
                            expando.TryAdd(names[i], values[i]);
                        }
                        currentResult.Add(expando);
                    }

                    yield return currentResult;
                } while (reader.NextResult());
            }
        }
    }

    public interface IDbMigrator
    {
        Version MigrationVersion { get; }
        void Migrate(DbConnection connection);
    }

    public class ScriptBasedDbMigrator : IDbMigrator
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

        public void Migrate(DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = SqlText;
                command.ExecuteNonQuery();
            }
        }
    }
}