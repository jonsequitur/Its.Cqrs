using System;
using System.Data.Entity;
using System.Data.SqlClient;
using FluentAssertions;
using Its.Configuration;
using Microsoft.Its.Domain.Sql.Migrations;
using NCrunch.Framework;
using Newtonsoft.Json;
using NUnit.Framework;
using Test.Domain.Ordering.Projections;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsMigrationsTestEventStore", "ItsCqrsMigrationsTestReadModels")]
    public class AzureDatabaseConfigurationTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            Settings.Sources = new ISettingsSource[]
            {
                new ConfigDirectorySettings(@"c:\dev\.config")
            };
            Settings.Deserialize = (type, json) => JsonConvert.DeserializeObject(json, type);
        }

        [Test]
        public void When_used_with_a_non_Azure_SQL_database_then_AzureSqlDbMigrator_is_applied_but_does_nothing()
        {
            // arrange
            Database.Delete(MigrationsTestReadModels.ConnectionString);

            using (var context = new MigrationsTestReadModels())
            {
                new CreateAndMigrate<MigrationsTestReadModels>().InitializeDatabase(context);

                var migrator = new AzureSqlDbMigrator(
                    new AzureSqlDatabaseServiceObjective("standard", "S0", 500),
                    migrationVersion: new Version("0.1.2.3"));

                // act
                context.EnsureDatabaseIsUpToDate(migrator);

                // assert
                var latestAppliedMigrationVersions = context.OpenConnection()
                                                            .GetLatestAppliedMigrationVersions();

                latestAppliedMigrationVersions
                    .Should()
                    .Contain(v => v.MigrationVersion.ToString() == "0.1.2.3");
            }
        }

        [Test]
        [Ignore("Integration tests"), NUnit.Framework.Category("Integration tests")]
        public void AzureSqlDatabase_EventStore_can_be_configured_using_a_migration()
        {
            var databaseSettings = Settings.Get<AzureSqlDatabaseSettings>();
            databaseSettings.DatabaseName = "ItsCqrsMigrationsTestEventStore";
            var connectionString = databaseSettings.BuildConnectionString();

            using (var context = new EventStoreDbContext(connectionString))
            {
                new EventStoreDatabaseInitializer<EventStoreDbContext>().InitializeDatabase(context);

                var migrator = new AzureSqlDbMigrator(
                    new AzureSqlDatabaseServiceObjective("standard", "S0", 500),
                    migrationVersion: new Version("0.0.42.1"));

                context.EnsureDatabaseIsUpToDate(migrator);

                context.OpenConnection()
                       .GetLatestAppliedMigrationVersions()
                       .Should()
                       .Contain(v => v.MigrationVersion.ToString() == "0.0.42.1");
            }
        }

        [Test]
        [Ignore("Integration tests"), NUnit.Framework.Category("Integration tests")]
        public void AzureSqlDatabase_ReadModel_can_be_configured_using_a_migration()
        {
            var databaseSettings = Settings.Get<AzureSqlDatabaseSettings>();
            databaseSettings.DatabaseName = "ItsCqrsMigrationsTestReadModels";
            var connectionString = databaseSettings.BuildConnectionString();

            using (var context = new MigrationsTestReadModels(connectionString, typeof(OrderTallyEntityModelConfiguration)))
            {
                new ReadModelDatabaseInitializer<MigrationsTestReadModels>().InitializeDatabase(context);

                var migrator = new AzureSqlDbMigrator(
                    new AzureSqlDatabaseServiceObjective("Premium", "P1", 10 * 1024),
                    migrationVersion: new Version("0.0.42.1"));

                context.EnsureDatabaseIsUpToDate(migrator);
                var sku = context.GetAzureDatabaseProperties();

                sku.Edition.Should().Be("Premium");
                sku.ServiceObjective.Should().Be("P1");

                context.OpenConnection()
                       .GetLatestAppliedMigrationVersions()
                       .Should()
                       .Contain(v => v.MigrationVersion.ToString() == "0.0.42.1");
            }
        }

        [Test]
        [Ignore("Integration tests"), NUnit.Framework.Category("Integration tests")]
        public void AzureSqlDatabase_can_be_configured_during_creation()
        {
            var databaseSettings = Settings.Get<AzureSqlDatabaseSettings>();
            databaseSettings.DatabaseName = "ItsCqrsPremiumDatabase";
            var connectionString = databaseSettings.BuildConnectionString();
            var sqlAzureDatabaseProperties = new AzureSqlDatabaseServiceObjective("Premium", "P1", 10 * 1024);

            using (var context = new MigrationsTestReadModels(connectionString, typeof(OrderTallyEntityModelConfiguration)))
            {
                new ReadModelDatabaseInitializer<MigrationsTestReadModels>()
                    .WithSqlAzureDatabaseProperties(sqlAzureDatabaseProperties)
                    .InitializeDatabase(context);

                var sku = context.GetAzureDatabaseProperties();

                sku.Edition.Should().Be("Premium");
                sku.ServiceObjective.Should().Be("P1");

                // Drop the expensive database
                context.Database.Connection.Close();
                context.Database.Delete();
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
                throw new ArgumentNullException(nameof(connectionString));
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