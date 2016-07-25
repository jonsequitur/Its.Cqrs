// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Its.Log.Instrumentation;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Migrations;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NCrunch.Framework;
using NUnit.Framework;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsTestsCommandScheduler")]
    [DisableCommandAuthorization]
    [UseSqlStorageForScheduledCommands]
    public class SqlCommandSchedulerDatabaseCleanupTests
    {
        private Guid aggregateId;
        private int sequenceNumber;

        static SqlCommandSchedulerDatabaseCleanupTests()
        {
            Formatter<MigrationResult>.RegisterForAllMembers();
        }

        [SetUp]
        public void SetUp()
        {
            aggregateId = Any.Guid();
            sequenceNumber = Any.PositiveInt();
            
            using (var db = CommandSchedulerDbContext())
            {
                db.Database.ExecuteSqlCommand("delete from PocketMigrator.AppliedMigrations where MigrationScope = 'CommandSchedulerCleanup'");
            }
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
            var clock = VirtualClock.Current;

            clock.AdvanceTo(DateTimeOffset.Parse("2046-06-07 07:07:41 PM"));

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
                                  c => c.UseConnectionString(TestDatabases.CommandScheduler.ConnectionString)
                                        .CleanUp(
                                            frequencyInDays: 3,
                                            completedCommandsOlderThan: 7.Days()));

                          configuration.StartBackgroundWork();
                      });

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var appliedMigrations = db.Database
                    .SqlQuery<string>(@"
select MigrationVersion from PocketMigrator.AppliedMigrations
where MigrationScope = 'CommandSchedulerCleanup'");

                appliedMigrations   
                    .Should()
                    .BeEquivalentTo("2046.6.7",
                                    "2046.6.10",
                                    "2046.6.13",
                                    "2046.6.16");
            }
        }

        private void Run(IDbMigrator migration)
        {
            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var results = migration.Migrate(db);

                Console.WriteLine(results.ToLogString());
            }
        }

        private bool ScheduledCommandExists()
        {
            using (var db = Configuration.Current.CommandSchedulerDbContext())
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
            var clockName = Configuration.Current.Container.Resolve<GetClockName>()(null);

            using (var db = Configuration.Current.CommandSchedulerDbContext())
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