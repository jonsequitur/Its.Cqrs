using System;
using System.Collections.Generic;
using FluentAssertions;
using Its.Log.Instrumentation;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using NUnit.Framework;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SqlCommandSchedulerTests_Legacy : SqlCommandSchedulerTests
    {
        private SqlCommandScheduler sqlCommandScheduler;

        [Test]
        public async Task SqlCommandScheduler_Activity_is_notified_when_a_command_is_delivered_via_Trigger()
        {
            // arrange
            var order = CommandSchedulingTests.CreateOrder();

            var activity = new List<ICommandSchedulerActivity>();

            order.Apply(new ShipOn(Clock.Now().Add(TimeSpan.FromDays(2))));
            await orderRepository.Save(order);

            using (sqlCommandScheduler.Activity.Subscribe(activity.Add))
            {
                // act
                await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(3));

                //assert 
                activity.Should()
                        .ContainSingle(a => a.ScheduledCommand.AggregateId == order.Id &&
                                            a is CommandSucceeded);
            }
        }

        protected override void ConfigureScheduler(Configuration configuration)
        {
            configuration.UseSqlCommandScheduling();
            sqlCommandScheduler = configuration.SqlCommandScheduler();
            sqlCommandScheduler.GetClockName = e => clockName;
            clockTrigger = sqlCommandScheduler;
            clockRepository = sqlCommandScheduler;

            disposables.Add(sqlCommandScheduler.Activity.Subscribe(s => Console.WriteLine("SqlCommandScheduler: " + s.ToLogString())));
        }

        protected override async Task SchedulerWorkComplete()
        {
            await sqlCommandScheduler.Done(clockName);
        }
    }
}