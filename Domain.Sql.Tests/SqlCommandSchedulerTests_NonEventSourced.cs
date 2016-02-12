// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public class SqlCommandSchedulerTests_NonEventSourced : SqlCommandSchedulerTests
    {
        private ICommandScheduler<CommandTarget> scheduler;
        private string clockName;
        private CompositeDisposable disposables;
        private EventStoreDbTest eventStoreDbTest;

        public SqlCommandSchedulerTests_NonEventSourced()
        {
            Command<CommandTarget>.AuthorizeDefault= (target, command) => true;
        }

        [SetUp]
        public void SetUp()
        {
            eventStoreDbTest = new EventStoreDbTest();
            clockName = Any.CamelCaseName();

            Clock.Reset();

            disposables = new CompositeDisposable
            {
                Disposable.Create(() => eventStoreDbTest.TearDown()),
                Disposable.Create(Clock.Reset)
            };

            var configuration = new Configuration();

            ConfigureScheduler(configuration);

            disposables.Add(ConfigurationContext.Establish(configuration));

            Console.WriteLine(new { clockName });
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        protected override void ConfigureScheduler(Configuration configuration)
        {
            disposables = new CompositeDisposable();
            clockName = Any.CamelCaseName();

            configuration.UseSqlStorageForScheduledCommands()
                         .UseInMemoryCommandTargetStore()
                         .TraceScheduledCommands()
                         .UseDependency<GetClockName>(_ => command => clockName);

            scheduler = configuration.CommandScheduler<CommandTarget>();
        }

        [Test]
        public override async Task When_a_clock_is_advanced_its_associated_commands_are_triggered()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            var store = Configuration.Current.Store<CommandTarget>();
            await store.Put(target);

            await scheduler.Schedule(target.Id,
                                     new TestCommand(),
                                     Clock.Now().AddDays(1));

            // act
            await Configuration.Current
                               .SchedulerClockTrigger()
                               .AdvanceClock(clockName: clockName,
                                             @by: TimeSpan.FromDays(1.1));

            //assert 
            target = await store.Get(target.Id);

            target.CommandsEnacted.Should().HaveCount(1);
        }
    }
}