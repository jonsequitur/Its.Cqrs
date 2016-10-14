// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Migrations;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using System.Data.SqlClient;
using System.Reactive.Disposables;
using NCrunch.Framework;
using Test.Domain.Ordering.Projections;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsMigrationsTestCommandScheduler","ItsCqrsMigrationsTestEventStore","ItsCqrsMigrationsTestReadModels")]
    public class DatabaseSetupAndMigrationTests
    {
        private const string CommandSchedulerConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsMigrationsTestCommandScheduler";
        
        private Version version = new Version(10, 0, 0);
        private CompositeDisposable disposables;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            disposables = new CompositeDisposable();

            var configuration = new Configuration()
                .UseSqlStorageForScheduledCommands(
                    c => c.UseConnectionString(CommandSchedulerConnectionString));

            disposables.Add(ConfigurationContext.Establish(configuration));

            Database.Delete(MigrationsTestEventStore.ConnectionString);
            Database.Delete(CommandSchedulerConnectionString);
            Database.Delete(MigrationsTestReadModels.ConnectionString);

            InitializeEventStore();
            InitializeCommandScheduler();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            disposables.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            version = new Version(version.Major, version.Minor, version.Build + 1);
        }

        [Test]
        public void event_store_etag_is_indexed()
        {
            using (var context = EventStoreDbContext())
            {
                var result = context.QueryDynamic(@"SELECT * FROM sys.indexes WHERE name='IX_ETag' AND object_id = OBJECT_ID('eventstore.events')").Single();
                result.Should().NotBeEmpty();
            }
        }

        [Test]
        public void event_store_etag_is_nullable()
        {
            using (var context = EventStoreDbContext())
            {
                var result = context.QueryDynamic(@"SELECT * FROM sys.columns WHERE name='ETag' AND object_id = OBJECT_ID('eventstore.events')").Single();
                bool is_nullable = result.Single().is_nullable;
                is_nullable.Should().BeTrue();
            }
        }

        [Test]
        public void event_store_id_is_indexed()
        {
            using (var context = EventStoreDbContext())
            {
                var result = context.QueryDynamic(@"SELECT * FROM sys.indexes WHERE name='IX_Id' AND object_id = OBJECT_ID('eventstore.events')").Single();
                result.Should().NotBeEmpty();
            }
        }

        [Test]
        public void event_store_id_streamname_type_is_indexed()
        {
            using (var context = EventStoreDbContext())
            {
                var result = context.QueryDynamic(@"SELECT * FROM sys.indexes WHERE name='IX_Id_StreamName_Type' AND object_id = OBJECT_ID('eventstore.events')").Single();
                result.Should().NotBeEmpty();
            }
        }

        [Test]
        public void When_a_migration_is_run_it_creates_a_record_in_the_migrations_table()
        {
            var appliedVersions = GetAppliedVersions(Configuration.Current.CommandSchedulerDbContext());

            appliedVersions
                .Should()
                .ContainSingle(v => v == "0.14.0");
        }

        [Test]
        public void When_a_migration_throws_then_the_change_is_rolled_back()
        {
            InitializeDatabase<MigrationsTestEventStore>();

            var columnName = Any.CamelCaseName(3);

            try
            {
                InitializeDatabase<MigrationsTestEventStore>(
                    new AnonymousMigrator(c =>
                    {
                        c.Database.ExecuteSqlCommand($@"alter table [eventstore].[events] add {columnName} nvarchar(50) null");
                        throw new DataMisalignedException();
                    }, version));
            }
            catch (DataMisalignedException)
            {
            }

            GetAppliedVersions(new MigrationsTestEventStore())
                .Should()
                .NotContain(s => s == version.ToString());

            using (var context = new MigrationsTestEventStore())
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

            InitializeDatabase<MigrationsTestEventStore>(migrator);
            InitializeDatabase<MigrationsTestEventStore>(migrator);

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

            InitializeDatabase<MigrationsTestEventStore>(second, first);

            calls.Should().ContainInOrder("first", "second");
        }

        [Test]
        public async Task Database_creations_in_a_race_do_not_throw()
        {
            var task1 = Task.Run(() => InitializeDatabase<MigrationsTestEventStore>());
            var task2 = Task.Run(() => InitializeDatabase<MigrationsTestEventStore>());

            await Task.WhenAll(task1, task2);

            using (var context = new MigrationsTestEventStore())
            {
                context.Database.Exists().Should().BeTrue();
            }
        }

        [Test]
        public async Task Migrations_in_a_race_do_not_throw()
        {
            InitializeDatabase<MigrationsTestEventStore>();

            var columnName = Any.CamelCaseName(3);
            var barrier = new Barrier(2);

            var migrator = new AnonymousMigrator(c =>
            {
                c.Database.ExecuteSqlCommand($@"alter table [eventstore].[events] add {columnName} nvarchar(50) null");
                barrier.SignalAndWait(10000);
            }, version);

            var task1 = Task.Run(() => InitializeDatabase<MigrationsTestEventStore>(migrator));
            var task2 = Task.Run(() => InitializeDatabase<MigrationsTestEventStore>(migrator));

            await Task.WhenAll(task1, task2);

            using (var context = new MigrationsTestEventStore())
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

            InitializeDatabase<MigrationsTestEventStore>(higherVersion);
            InitializeDatabase<MigrationsTestEventStore>(lowerVersion);

            var appliedMigrations = GetAppliedVersions(new MigrationsTestEventStore());

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

            InitializeDatabase<MigrationsTestEventStore>(higherVersion);
            InitializeDatabase<MigrationsTestEventStore>(lowerVersion);

            var appliedMigrations = GetAppliedVersions(new MigrationsTestEventStore());

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

            InitializeDatabase<MigrationsTestEventStore>(migrator);

            GetAppliedVersions(new MigrationsTestEventStore())
                .Should().NotContain(v => v == version.ToString());
        }

        [Test]
        public void Command_scheduler_database_contains_a_default_clock()
        {
            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                db.Clocks.Should().ContainSingle(c => c.Name == "default");
            }
        }

        [Test]
        public void When_a_read_only_user_connects_to_the_event_store_Then_migrations_should_not_be_run()
        {
            var userName = Any.CamelCaseName();
            var password = "Password#1";
            var loginName = userName;
            var user = new DbReadonlyUser(userName, loginName);
            var builder = new SqlConnectionStringBuilder();
            var migrated = false;

            using (var db = EventStoreDbContext())
            {
                db.Database.ExecuteSqlCommand($"CREATE LOGIN [{userName}] WITH PASSWORD = '{password}';");
                db.CreateReadonlyUser(user);

                builder.ConnectionString = db.Database.Connection.ConnectionString;
                builder.IntegratedSecurity = false;
                builder.UserID = user.LoginName;
                builder.Password = password;
            }

            using (var db = new EventStoreDbContext(builder.ConnectionString))
            {
                IDbMigrator[] migrations = { new AnonymousMigrator(c => migrated = true, new Version(1, 0), Any.CamelCaseName()) };
                var initializer = new EventStoreDatabaseInitializer<EventStoreDbContext>(migrations);
                initializer.InitializeDatabase(db);
            }

            migrated.Should().BeFalse();
        }

        [Test]
        public void ReadModelDbContext_drops_the_database_and_recreates_it_when_the_schema_has_changed()
        {
            // arrange
            Database.SetInitializer(new ReadModelDatabaseInitializer<MigrationsTestReadModels>());
            
            using (var db = new MigrationsTestReadModels(
                typeof(OrderTallyEntityModelConfiguration)))
            {
                db.Database.Initialize(true);

                db.Set<OrderTally>().Add(new OrderTally
                {
                    Count = 1,
                    Status = Any.Word()
                });

                db.SaveChanges();

                db.Set<OrderTally>().Count().Should().Be(1);
            }

            using (var db = new MigrationsTestReadModels(
                typeof(OrderTallyEntityModelConfiguration),
                typeof(ProductInventoryEntityModelConfiguration)))
            {
                // act
                db.Database.Initialize(true);

                // assert
                db.Set<OrderTally>().Count().Should().Be(0);
                db.Set<ProductInventory>().Count().Should().Be(0);
            }
        }

        [Test]
        public void ReadModelDbContext_drops_the_database_and_recreates_it_when_the_version_has_changed()
        {
            // arrange
            Database.SetInitializer(new ReadModelDatabaseInitializer<MigrationsTestReadModels>(new Version("1.0")));

            using (var db = new MigrationsTestReadModels(
                typeof(OrderTallyEntityModelConfiguration)))
            {
                db.Database.Initialize(true);

                db.Set<OrderTally>().Add(new OrderTally
                {
                    Count = 1,
                    Status = Any.Word()
                });

                db.SaveChanges();

                db.Set<OrderTally>().Count().Should().Be(1);
            }

            Database.SetInitializer(new ReadModelDatabaseInitializer<MigrationsTestReadModels>(new Version("1.1")));

            using (var db = new MigrationsTestReadModels(
                typeof(OrderTallyEntityModelConfiguration)))
            {
                // act
                db.Database.Initialize(true);

                // assert
                db.Set<OrderTally>().Count().Should().Be(0);
            }
        }

        [Test]
        public void ReadModelDbContext_drops_the_database_and_recreates_it_when_the_prior_version_has_no_migrations()
        {
            // arrange
            Database.SetInitializer(new DropCreateDatabaseAlways<MigrationsTestReadModels>());

            using (var db = new MigrationsTestReadModels(
                typeof(OrderTallyEntityModelConfiguration)))
            {
                db.Database.Initialize(true);

                db.Set<OrderTally>().Add(new OrderTally
                {
                    Count = 1,
                    Status = Any.Word()
                });

                db.SaveChanges();

                db.Set<OrderTally>().Count().Should().Be(1);
            }

            Database.SetInitializer(new ReadModelDatabaseInitializer<MigrationsTestReadModels>(new Version("1.1")));

            using (var db = new MigrationsTestReadModels(
                typeof(OrderTallyEntityModelConfiguration)))
            {
                // act
                db.Database.Initialize(true);

                // assert
                db.Set<OrderTally>().Count().Should().Be(0);
            }
        }

        [Test]
        public void ScriptBasedDbMigrator_applies_migrations_within_a_transaction()
        {
            //arrange
            var scope = Any.CamelCaseName();
            var etag = Any.CamelCaseName();

            string sql = $@"INSERT [Scheduler].[ETag] 
                                        ([Scope], 
                                         [ETagValue],
                                         [CreatedDomainTime],
                                         [CreatedRealTime]) 
                                 VALUES ('{scope}',
                                         '{etag}',
                                         '{Clock.Now()
                }',
                                         '{DateTimeOffset.UtcNow}')";

            // migrator attempts to insert the same etag twice, which should trigger a unique constraint error
            var migrator =
                new ScriptBasedDbMigrator(sql + "\n" + sql, 
                    scope: scope,
                    migrationVersion: version);

            // act
            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                try
                {
                    db.EnsureDatabaseIsUpToDate(migrator);
                }
                catch (SqlException exception)
                    when (exception.Message.Contains("Cannot insert duplicate key row in object 'Scheduler.ETag'"))
                {
                    Console.WriteLine(exception);
                }
            }

            // assert
            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var etagsInserted = db.ETags
                                      .Where(e => e.Scope == scope && e.ETagValue == etag).ToArray();
                etagsInserted.Should().BeEmpty();
            }

            GetAppliedVersions(Configuration.Current.CommandSchedulerDbContext())
                .Should()
                .NotContain(v => v == version.ToString());
        }

        private static string[] GetAppliedVersions(DbContext context)
        {
            using (context)
            {
                var connection = context.OpenConnection();

                try
                {
                    return connection
                        .QueryDynamic(
                            @"SELECT MigrationVersion from PocketMigrator.AppliedMigrations")
                        .Single()
                        .Select(x => (string) x.MigrationVersion)
                        .ToArray();
                }
                catch (SqlException exception)
                {
                    if (exception.Number == 208) // AppliedMigrations table is not present
                    {
                        return new string[0];
                    }

                    throw;
                }
            }
        }
        
        private void InitializeEventStore()
        {
            using (var context = EventStoreDbContext())
            {
                new EventStoreDatabaseInitializer<EventStoreDbContext>().InitializeDatabase(context);
            }
        }

        private void InitializeCommandScheduler()
        {
            using (var context = Configuration.Current.CommandSchedulerDbContext())
            {
                new CommandSchedulerDatabaseInitializer().InitializeDatabase(context);
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

    public class MigrationsTestEventStore : EventStoreDbContext
    {
        public const string ConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsMigrationsTestEventStore";

        public MigrationsTestEventStore() : base(ConnectionString)
        {
        }
    }

    public class MigrationsTestReadModels : ReadModelDbContext
    {
        private readonly IEnumerable<Type> entityModelConfigurationTypes;

        public const string ConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsMigrationsTestReadModels";

        public MigrationsTestReadModels() : base(ConnectionString)
        {
           
        }

        public MigrationsTestReadModels(params Type[] entityModelConfigurationTypes) : base(ConnectionString, BuildModel(entityModelConfigurationTypes))
        {
            if (entityModelConfigurationTypes == null)
            {
                throw new ArgumentNullException(nameof(entityModelConfigurationTypes));
            }
            this.entityModelConfigurationTypes = entityModelConfigurationTypes;
        }

        public MigrationsTestReadModels(string connectionString, params Type[] entityModelConfigurationTypes) : base(connectionString, BuildModel(entityModelConfigurationTypes))
        {
            if (entityModelConfigurationTypes == null)
            {
                throw new ArgumentNullException(nameof(entityModelConfigurationTypes));
            }
            this.entityModelConfigurationTypes = entityModelConfigurationTypes;
        }

        private static DbCompiledModel BuildModel(Type[] types)
        {
            var builder = new DbModelBuilder();

            foreach (var configuration in types
                .Select(Domain.Configuration.Current.Container.Resolve)
                .Cast<IEntityModelConfiguration>().ToArray())
            {
                configuration.ConfigureModel(builder.Configurations);
            }

            DbModel model = builder.Build(new SqlConnection(ConnectionString));
            return model.Compile();
        }

        protected override IEnumerable<Type> GetEntityModelConfigurationTypes()
        {
            return entityModelConfigurationTypes.OrEmpty();
        }
    }

    public class AnonymousMigrator : IDbMigrator
    {
        private readonly Func<DbContext, MigrationResult> migrate;

        public AnonymousMigrator(Action<DbContext> migrate, Version version, string scope = "Test")
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
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

        public AnonymousMigrator(Func<DbContext, MigrationResult> migrate, Version version, string scope = "Test")
        {
            if (migrate == null)
            {
                throw new ArgumentNullException(nameof(migrate));
            }
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }
            this.migrate = migrate;
            MigrationScope = scope;
            MigrationVersion = version;
        }

        public string MigrationScope { get; }

        public Version MigrationVersion { get; }

        public MigrationResult Migrate(DbContext connection) => migrate(connection);
    }
}