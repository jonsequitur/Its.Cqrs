// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Its.Domain.Serialization;

namespace Microsoft.Its.Domain.Sql
{
    public static class DbContextExtensions
    {
        public static void Unique<TProjection>(
            this DbContext context,
            Expression<Func<TProjection, object>> member,
            string schema = "dbo") 
            where TProjection : class
        {
            context.Database.ExecuteSqlCommand(AddUniqueIndex(context, member, schema));
        }

        internal static string AddUniqueIndex<TProjection>(
            this DbContext context, 
            Expression<Func<TProjection, object>> member,
            string schema = "dbo")
            where TProjection : class
        {
            var tableName = context.TableNameFor<TProjection>();
            return string.Format("CREATE UNIQUE INDEX IX_{0}_{1} ON {2}.{0} ({1})",
                                 tableName,
                                 member.MemberName(),
                                 schema);
        }

        public static void Unique<TProjection>(
            this DbContext context,
            Expression<Func<TProjection, object>> member1,
            Expression<Func<TProjection, object>> member2,
            string schema = "dbo") where TProjection : class
        {
            context.Database.ExecuteSqlCommand(
                string.Format("CREATE UNIQUE INDEX IX_{0}_{1}_{2} ON {3}.{0} ({1}, {2})",
                              context.TableNameFor<TProjection>(),
                              member1.MemberName(),
                              member2.MemberName(),
                              schema));
        }

        public static void SeedFromFile(this EventStoreDbContext context, FileInfo file)
        {
            using (var stream = file.OpenRead())
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                var events = Serializer.FromJsonToEvents(json).ToArray();

                foreach (var e in events)
                {
                    context.Events.Add(e.ToStorableEvent());

                    // it's necessary to save at every event to preserve ordering, otherwise EF will reorder them
                    context.SaveChanges();
                }
            }
        }

        private static string TableNameFor<T>(this DbContext context) where T : class
        {
            var objectContext = ((IObjectContextAdapter) context).ObjectContext;

            var sql = objectContext.CreateObjectSet<T>().ToTraceString();

            var match = new Regex(@"FROM ([\[\]a-z0-9]*\.)?(?<table>.*) AS", RegexOptions.IgnoreCase)
                .Match(sql);

            var matched = match.Groups["table"].Value;

            return Regex.Replace(matched, @"[\[\]\.]", "");
        }

        internal static DbConnection OpenConnection(this DbContext context)
        {
            var connection = context.Database.Connection;
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            return connection;
        }

        public static void CreateDatabase(this DbContext context, int dbSizeInGB, string edition, string serviceObjective)
        {
            var connectionString = context.Database.Connection.ConnectionString;
            var databaseName = context.Database.Connection.Database;

            if (connectionString.Contains("database.windows.net"))
            {
                var connstrBldr = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master"
                };

                var dbCreationCmd = string.Format("CREATE DATABASE [{0}] (MAXSIZE={1}GB, EDITION='{2}', SERVICE_OBJECTIVE='{3}')",
                        databaseName, dbSizeInGB, edition, serviceObjective);

                ExecuteNonQuery(connstrBldr.ConnectionString, dbCreationCmd);
                WaitUntilDatabaseCreated(connstrBldr.ConnectionString, databaseName);

                context.Database.Initialize(force: true);
            }
            else
            {
                context.Database.Create();
            }
        }

        private static void WaitUntilDatabaseCreated(string connString, string databaseName)
        {
            // wait up to 60 seconds
            var sleepInSeconds = 2;
            var retryCount = 30;
            var dbStatus = "CREATING";

            while (true)
            {
                if (retryCount <= 0)
                {
                    throw new DataException("dbStatus is not ONLINE after 60 seconds, status = " + dbStatus);
                }

                dbStatus = Query(connString, string.Format("SELECT state_desc FROM sys.databases WHERE name = '{0}'", databaseName));
                if (dbStatus.Equals("ONLINE", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                retryCount--;
                Thread.Sleep(TimeSpan.FromSeconds(sleepInSeconds));
            }
        }

        private static void ExecuteNonQuery(string connString, string commandText)
        {
            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.ExecuteNonQuery();
            }
        }

        private static string Query(string connString, string query)
        {
            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                return (string) cmd.ExecuteScalar();
            }
        }
    }
}
