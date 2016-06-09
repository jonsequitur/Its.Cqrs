// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity.Infrastructure;
using FluentAssertions;
using Its.Log.Instrumentation;
using System.Linq;
using System.Reactive.Disposables;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Migrations;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NCrunch.Framework;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsTestsCommandScheduler")]
    public class SqlCommandSchedulerDatabaseCleanupTests
    {
        private CompositeDisposable disposables;
        private string clockName;
        private Guid aggregateId;
        private int sequenceNumber;

        static SqlCommandSchedulerDatabaseCleanupTests()
        {
            Formatter<MigrationResult>.RegisterForAllMembers();
        }

        [SetUp]
        public void SetUp()
        {
            TestDatabases.SetConnectionStrings();

            aggregateId = Any.Guid();
            sequenceNumber = Any.PositiveInt();

            if (clockName == null)
            {
                clockName = Any.CamelCaseName();

                using (var db = new CommandSchedulerDbContext())
                {
                    db.Clocks.Add(new CommandScheduler.Clock
                    {
                        Name = clockName,
                        StartTime = Clock.Now(),
                        UtcNow = Clock.Now()
                    });

                    db.SaveChanges();
                }
            }

            disposables = new CompositeDisposable
            {
                ConfigurationContext.Establish(new Configuration()
                                                   .UseSqlStorageForScheduledCommands())
            };
            
            using (var db = new CommandSchedulerDbContext())
            {
                db.Database.ExecuteSqlCommand("delete from PocketMigrator.AppliedMigrations where MigrationScope = 'CommandSchedulerCleanup'");
            }
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public void Cleanup_does_not_delete_commands_that_have_not_been_applied()
        {
            // arrange: set up some test data
            WriteScheduledCommand(
                finalAttemptTime: null,
                appliedTime: null,
                dueTime: Clock.Now().Subtract(1.Days()));

            // act
            var migration = new CommandSchedulerCleanupMigration(
                frequencyInDays: 1,
                completedCommandsOlderThan: 7.Days());
            Run(migration);

            // assert
            ScheduledCommandExists().Should().BeTrue();
        }

        [Test]
        public void Cleanup_deletes_commands_that_were_applied_before_the_cutoff()
        {
            // arrange: set up some test data
            WriteScheduledCommand(
                finalAttemptTime: null,
                appliedTime: Clock.Now().Subtract(10.Days()),
                dueTime: Clock.Now().Subtract(100.Days()));

            // act
            var migration = new CommandSchedulerCleanupMigration(
                frequencyInDays: 1,
                completedCommandsOlderThan: 7.Days());
            Run(migration);

            // assert
            ScheduledCommandExists().Should().BeFalse();
        }

        [Test]
        public void Cleanup_does_not_delete_commands_that_were_timed_out_before_the_cutoff()
        {
            // arrange: set up some test data
            WriteScheduledCommand(
                finalAttemptTime: Clock.Now().Subtract(10.Days()),
                appliedTime: null,
                dueTime: Clock.Now().Subtract(100.Days()));

            // act
            var migration = new CommandSchedulerCleanupMigration(
                frequencyInDays: 1,
                completedCommandsOlderThan: 7.Days());
            Run(migration);

            // assert
            ScheduledCommandExists().Should().BeTrue();
        }

        [Test]
        public void Cleanup_can_be_scheduled_periodically()
        {
            var clock = VirtualClock.Start(DateTimeOffset.Parse("2016-06-07 07:07:41 PM"));
            disposables.Add(clock);

            // arrange: set up some test data
            WriteScheduledCommand(
                finalAttemptTime: null,
                appliedTime: Clock.Now().Subtract(10.Days()),
                dueTime: Clock.Now().Subtract(100.Days()));

            // act
            Enumerable.Range(1, 10)
                      .ForEach(_ =>
                      {
                          clock.AdvanceBy(1.Days());

                          var configuration = new Configuration()
                              .UseSqlStorageForScheduledCommands(
                                  c => c.CleanUp(
                                      frequencyInDays: 3,
                                      completedCommandsOlderThan: 7.Days()));

                          configuration.StartBackgroundWork();
                      });

            using (var db = new CommandSchedulerDbContext())
            {
                var appliedMigrations = db.Database
                    .SqlQuery<string>(@"
select MigrationVersion from PocketMigrator.AppliedMigrations
where MigrationScope = 'CommandSchedulerCleanup'");

                appliedMigrations   
                    .Should()
                    .BeEquivalentTo("2016.6.8",
                                    "2016.6.11",
                                    "2016.6.14",
                                    "2016.6.17");
            }
        }

        private void Run(IDbMigrator migration)
        {
            using (var db = new CommandSchedulerDbContext())
            {
                var results = migration.Migrate(db);

                Console.WriteLine(results.ToLogString());
            }
        }

        private bool ScheduledCommandExists()
        {
            using (var db = new CommandSchedulerDbContext())
            {
                return db.ScheduledCommands.Any(c =>
                                                c.AggregateId == aggregateId &&
                                                c.SequenceNumber == sequenceNumber);
            }
        }

        private void WriteScheduledCommand(
            DateTimeOffset? appliedTime,
            DateTimeOffset? finalAttemptTime,
            DateTimeOffset? dueTime)
        {
            using (var db = new CommandSchedulerDbContext())
            {
                db.ScheduledCommands.Add(new ScheduledCommand
                {
                    AggregateType = GetType().Name,
                    DueTime = dueTime,
                    AppliedTime = appliedTime,
                    Clock = db.Clocks.Single(c => c.Name == clockName),
                    AggregateId = aggregateId,
                    FinalAttemptTime = finalAttemptTime,
                    SequenceNumber = sequenceNumber,
                    SerializedCommand = new object().ToJson(),
                    CreatedTime = Clock.Now().Subtract(10.Days())
                });

                db.SaveChanges();
            }
        }
    }
}