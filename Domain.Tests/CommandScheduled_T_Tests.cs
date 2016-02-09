using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    public class CommandScheduled_T_Tests : ScheduledCommandTests
    {
        protected override IScheduledCommand<T> CreateScheduledCommand<T>(
            ICommand<T> command,
            Guid aggregateId,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null)
        {
            return new CommandScheduled<T>
            {
                Command = command,
                AggregateId = aggregateId,
                DueTime = dueTime,
                DeliveryPrecondition = deliveryDependsOn
            };
        }

        [Test]
        public async Task Event_ETag_cannot_be_the_same_as_the_command_etag()
        {
            var etag1 = Any.Word().ToETag();

            CommandScheduled<Order> scheduled;

            using (CommandContext.Establish(new TestCommand(etag1)))
            {
                scheduled = new CommandScheduled<Order>
                {
                    AggregateId = Any.Guid(),
                    Command = new AddItem()
                };
            }

            scheduled.Command.ETag.Should().NotBeEmpty();
            scheduled.ETag.Should().NotBeEmpty();
            scheduled.ETag.Should().NotBe(scheduled.Command.ETag);
        }
    }
}