// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Migrations;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class DatabaseSetupAndMigrationTests
    {
        private const string EventStoreConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsMigrationsTestEventStore";
        private const string CommandSchedulerConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsMigrationsTestCommandScheduler";

        private Version version = new Version(10, 0, 0);

        [TestFixtureSetUp]
        public void Init()
        {
            EventStoreDbContext.NameOrConnectionString = EventStoreConnectionString;
            Database.Delete(EventStoreConnectionString);
            Database.Delete(CommandSchedulerConnectionString);
            Database.Delete(MigrationsTestDbContext.ConnectionString);
            InitializeEventStore();
            InitializeCommandScheduler();
        }

        [SetUp]
        public void SetUp()
        {
            version = new Version(version.Major, version.Minor, version.Build + 1);
        }

        [Test]
        public void event_store_etag_is_indexed()
        {
            using (var context = new EventStoreDbContext())
            {
                var result = context.QueryDynamic(@"SELECT * FROM sys.indexes WHERE name='IX_ETag' AND object_id = OBJECT_ID('eventstore.events')").Single();
                result.Should().NotBeEmpty();
            }
        }

        [Test]
        public void event_store_etag_is_nullable()
        {
            using (var context = new EventStoreDbContext())
            {
                var result = context.QueryDynamic(@"SELECT * FROM sys.columns WHERE name='ETag' AND object_id = OBJECT_ID('eventstore.events')").Single();
                bool is_nullable = result.Single().is_nullable;
                is_nullable.Should().BeTrue();
            }
        }

        [Test]
        public void event_store_stream_name_is_indexed()
        {
            using (var context = new EventStoreDbContext())
            {
                var result = context.QueryDynamic(@"SELECT * FROM sys.indexes WHERE name='IX_StreamName' AND object_id = OBJECT_ID('eventstore.events')").Single();
                result.Should().NotBeEmpty();
            }
        }

        [Test]
        public void event_store_type_is_indexed()
        {
            using (var context = new EventStoreDbContext())
            {
                var result = context.QueryDynamic(@"SELECT * FROM sys.indexes WHERE name='IX_Type' AND object_id = OBJECT_ID('eventstore.events')").Single();
                result.Should().NotBeEmpty();
            }
        }

        [Test]
        public void event_store_type_and_id_are_indexed()
        {
            using (var context = new EventStoreDbContext())
            {
                var result = context.QueryDynamic(@"SELECT * FROM sys.indexes WHERE name='IX_Id_and_Type' AND object_id = OBJECT_ID('eventstore.events')").Single();
                result.Should().NotBeEmpty();
            }
        }

        [Test]
        public void When_a_migration_is_run_it_creates_a_record_in_the_migrations_table()
        {
            var appliedVersions = GetAppliedVersions<EventStoreDbContext>();

            appliedVersions
                .Should()
                .ContainSingle(v => v == "0.14.0");
        }

        [Test]
        public void When_a_migration_throws_then_the_change_is_rolled_back()
        {
            InitializeDatabase<MigrationsTestDbContext>();

            var columnName = Any.CamelCaseName(3);

            try
            {
                InitializeDatabase<MigrationsTestDbContext>(
                    new AnonymousMigrator(c =>
                    {
                        c.Execute(string.Format(@"alter table [eventstore].[events] add {0} nvarchar(50) null", columnName));
                        throw new DataMisalignedException();
                    }, version));
            }
            catch (DataMisalignedException)
            {
            }

            GetAppliedVersions<MigrationsTestDbContext>().Should().NotContain(s => s == version.ToString());

            using (var context = new MigrationsTestDbContext())
            {
                var result = context.QueryDynamic(
                    @"SELECT * FROM sys.columns WHERE name='@columnName'",
                    new Dictionary<string, object> { { "columnName", columnName } }).Single();
                result.Should().BeEmpty();
            }
        }

        [Test]
        public void Migrations_are_not_run_more_than_once()
        {
            var callCount = 0;
            var migrator = new AnonymousMigrator(c => { callCount++; }, version);

            InitializeDatabase<MigrationsTestDbContext>(migrator);
            InitializeDatabase<MigrationsTestDbContext>(migrator);

            callCount.Should().Be(1);
        }

        [Test]
        public void Migrations_run_in_the_specified_order()
        {
            var calls = new List<string>();

            var first = new AnonymousMigrator(c => { calls.Add("first"); },
                                              new Version(version.Major, version.Minor, version.Build, 1));
            var second = new AnonymousMigrator(c => { calls.Add("second"); },
                                               new Version(version.Major, version.Minor, version.Build, 2));

            InitializeDatabase<MigrationsTestDbContext>(second, first);

            calls.Should().ContainInOrder(new[]
            {
                "first",
                "second"
            });
        }

        [Test]
        public async Task Database_creations_in_a_race_do_not_throw()
        {
            var task1 = Task.Run(() => InitializeDatabase<MigrationsTestDbContext>());
            var task2 = Task.Run(() => InitializeDatabase<MigrationsTestDbContext>());

            await Task.WhenAll(task1, task2);

            using (var context = new MigrationsTestDbContext())
            {
                context.Database.Exists().Should().BeTrue();
            }
        }

        [Test]
        public async Task Migrations_in_a_race_do_not_throw()
        {
            InitializeDatabase<MigrationsTestDbContext>();

            var columnName = Any.CamelCaseName(3);
            var barrier = new Barrier(2);

            var migrator = new AnonymousMigrator(c =>
            {
                c.Execute(string.Format(@"alter table [eventstore].[events] add {0} nvarchar(50) null", columnName));
                barrier.SignalAndWait(10000);
            }, version);

            var task1 = Task.Run(() => InitializeDatabase<MigrationsTestDbContext>(migrator));
            var task2 = Task.Run(() => InitializeDatabase<MigrationsTestDbContext>(migrator));

            await Task.WhenAll(task1, task2);

            using (var context = new MigrationsTestDbContext())
            {
                var result = context.QueryDynamic(
                    @"SELECT * FROM sys.columns WHERE name='@columnName'",
                    new Dictionary<string, object> { { "columnName", columnName } }).Single();
                result.Should().BeEmpty();
            }
        }

        [Test]
        public void A_migration_with_an_earlier_version_number_can_be_applied_later_if_they_have_different_scopes()
        {
            var higherVersion = new AnonymousMigrator(
                c => { }, 
                new Version(version.Major, version.Minor, version.Build, 2),
                "Scope1");
            var lowerVersion = new AnonymousMigrator(
                c => { }, 
                new Version(version.Major, version.Minor, version.Build),
                "Scope2");

            InitializeDatabase<MigrationsTestDbContext>(higherVersion);
            InitializeDatabase<MigrationsTestDbContext>(lowerVersion);

            var appliedMigrations = GetAppliedVersions<MigrationsTestDbContext>();

            appliedMigrations.Should().Contain(m => m == version + ".2");
            appliedMigrations.Should().Contain(m => m == version.ToString());
        }

        [Test]
        public void A_migration_with_an_earlier_version_number_cannot_be_applied_later_if_they_have_the_same_scope()
        {
            var higherVersion = new AnonymousMigrator(
                c => { }, 
                new Version(version.Major, version.Minor, version.Build, 2),
                "Test"
            );
            var lowerVersion = new AnonymousMigrator(
                c => { }, 
                new Version(version.Major, version.Minor, version.Build),
                "Test"
            );

            InitializeDatabase<MigrationsTestDbContext>(higherVersion);
            InitializeDatabase<MigrationsTestDbContext>(lowerVersion);

            var appliedMigrations = GetAppliedVersions<MigrationsTestDbContext>();

            appliedMigrations.Should().Contain(m => m == version + ".2");
            appliedMigrations.Should().NotContain(m => m == version.ToString());
        }

        [Test]
        public void When_a_migration_signals_that_it_was_not_applied_then_no_record_is_created_in_the_migrations_table()
        {
            var migrator = new AnonymousMigrator(c => new MigrationResult
            {
                MigrationWasApplied = false
            }, version);

            InitializeDatabase<MigrationsTestDbContext>(migrator);

            GetAppliedVersions<MigrationsTestDbContext>()
                .Should().NotContain(v => v == version.ToString());
        }

        [Test]
        public async Task Command_scheduler_database_contains_a_default_clock()
        {
            using (var db = new CommandSchedulerDbContext(CommandSchedulerConnectionString))
            {
                db.Clocks.Should().ContainSingle(c => c.Name == "default");
            }
        }

        private static IEnumerable<string> GetAppliedVersions<TContext>()
            where TContext : DbContext, new()
        {
            using (var context = new TContext())
            {
                return context.OpenConnection().GetAppliedMigrationVersions();
            }
        }

        private void InitializeEventStore()
        {
            using (var context = new EventStoreDbContext())
            {
                new EventStoreDatabaseInitializer<EventStoreDbContext>().InitializeDatabase(context);
            }
        }
        private void InitializeCommandScheduler()
        {
            using (var context = new CommandSchedulerDbContext(CommandSchedulerConnectionString))
            {
                new EventStoreDatabaseInitializer<CommandSchedulerDbContext>().InitializeDatabase(context);
            }
        }

        private void InitializeDatabase<TContext>(params IDbMigrator[] migrators)
            where TContext : DbContext, new()
        {
            using (var context = new TContext())
            {
                new CreateAndMigrate<TContext>(migrators).InitializeDatabase(context);
            }
        }
    }

    public class MigrationsTestDbContext : EventStoreDbContext
    {
        public const string ConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsMigrationsTest";

        public MigrationsTestDbContext() : base(ConnectionString)
        {
        }
    }

    public class AnonymousMigrator : IDbMigrator
    {
        private readonly Func<IDbConnection, MigrationResult> migrate;

        public AnonymousMigrator(Action<IDbConnection> migrate, Version version, string scope = "Test")
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }

            this.migrate = connection =>
            {
                migrate(connection);
                return new MigrationResult
                {
                    MigrationWasApplied = true
                };
            };
            MigrationVersion = version;
            MigrationScope = scope;
        }

        public AnonymousMigrator(Func<IDbConnection, MigrationResult> migrate, Version version, string scope = "Test")
        {
            if (migrate == null)
            {
                throw new ArgumentNullException("migrate");
            }
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
            this.migrate = migrate;
            MigrationScope = scope;
            MigrationVersion = version;
        }

        public string MigrationScope { get; private set; }

        public Version MigrationVersion { get; private set; }

        public MigrationResult Migrate(IDbConnection connection)
        {
            return migrate(connection);
        }
    }
}