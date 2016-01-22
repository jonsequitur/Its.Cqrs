using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using FluentAssertions;
using Its.Configuration;
using Microsoft.Its.Domain.Sql.Migrations;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [Ignore("Integration tests")]
    [TestFixture, Category("Integration tests")]
    public class AzureDatabaseConfigurationTests
    {
        [TestFixtureSetUp]
        public void Init()
        {
            Settings.Sources = new ISettingsSource[]
            {
                new ConfigDirectorySettings(@"c:\dev\.config")
            };
            Settings.Deserialize = (type, json) => JsonConvert.DeserializeObject(json, type);
        }

        [Test]
        public async Task AzureSqlDatabase_can_be_configured_using_a_migration()
        {
            var databaseSettings = Settings.Get<AzureSqlDatabaseSettings>();
            databaseSettings.DatabaseName = "ItsCqrsMigrationsTest";
            var connectionString = databaseSettings.BuildConnectionString();

            using (var context = new EventStoreDbContext(connectionString))
            {
                new EventStoreDatabaseInitializer<EventStoreDbContext>().InitializeDatabase(context);

                var migrator = new AzureSqlDbMigrator(
                    "S0",
                    "10 GB",
                    new Version("0.0.42.1"));
                context.EnsureDatabaseSchemaIsUpToDate(migrator);

                context.OpenConnection()
                       .GetAppliedMigrationVersions()
                       .Should()
                       .Contain("0.0.42.1");
            }
        }
    }

    public class AzureSqlDatabaseSettings
    {
        private readonly string connectionString;

        public AzureSqlDatabaseSettings(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }
            this.connectionString = connectionString;
        }

        public string DatabaseName { get; set; }

        public string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            if (!string.IsNullOrWhiteSpace(DatabaseName))
            {
                builder.InitialCatalog = DatabaseName;
            }

            return builder.ConnectionString;
        }
    }
}