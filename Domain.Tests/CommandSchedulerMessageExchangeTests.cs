// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Its.Log.Instrumentation;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public class CommandSchedulerMessageExchangeTests
    {
        private CompositeDisposable disposables;
        private Configuration configuration;
        private IEventSourcedRepository<MarcoPoloPlayerWhoIsIt> itRepo;

        [SetUp]
        public void SetUp()
        {
            disposables = new CompositeDisposable
            {
                VirtualClock.Start()
            };

            configuration = new Configuration()
                .UseInMemoryCommandScheduling()
                .UseInMemoryEventStore(traceEvents: true);

            itRepo = configuration.Repository<MarcoPoloPlayerWhoIsIt>();

            disposables.Add(ConfigurationContext.Establish(configuration));
            disposables.Add(configuration);
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public async Task Scatter_gather_produces_a_unique_etag_per_sent_command()
        {
            var repo = configuration.Repository<MarcoPoloPlayerWhoIsIt>();
            var it = new MarcoPoloPlayerWhoIsIt();
            await repo.Save(it);

            var numberOfPlayers = 6;
            var players = Enumerable.Range(1, numberOfPlayers)
                                    .Select(_ => new MarcoPoloPlayerWhoIsNotIt());

            foreach (var player in players)
            {
                var joinGame = new MarcoPoloPlayerWhoIsNotIt.JoinGame
                {
                    IdOfPlayerWhoIsIt = it.Id
                };
                await player.ApplyAsync(joinGame).AndSave();
            }

            await repo.Refresh(it);

            await it.ApplyAsync(new MarcoPoloPlayerWhoIsIt.SayMarco()).AndSave();

            await repo.Refresh(it);

            it.Events()
              .OfType<MarcoPoloPlayerWhoIsIt.HeardPolo>()
              .Count()
              .Should()
              .Be(numberOfPlayers);
        }

        [Test]
        public async Task Multiple_scheduled_commands_having_the_some_causative_command_etag_have_repeatable_and_unique_etags()
        {
            var scheduled = new List<ICommand>();
            string[] firstPassEtags;
            string[] secondPassEtags;

            configuration.AddToCommandSchedulerPipeline<MarcoPoloPlayerWhoIsIt>(async (cmd, next) =>
            {
                scheduled.Add(cmd.Command);
                await next(cmd);
            });
            configuration.AddToCommandSchedulerPipeline<MarcoPoloPlayerWhoIsNotIt>(async (cmd, next) =>
            {
                scheduled.Add(cmd.Command);
                await next(cmd);
            });

            var it = new MarcoPoloPlayerWhoIsIt()
                .Apply(new MarcoPoloPlayerWhoIsIt.AddPlayer { PlayerId = Any.Guid() })
                .Apply(new MarcoPoloPlayerWhoIsIt.AddPlayer { PlayerId = Any.Guid() });
            Console.WriteLine("[Saving]");
            await itRepo.Save(it);

            var sourceEtag = Any.Guid().ToString();

            await it.ApplyAsync(new MarcoPoloPlayerWhoIsIt.KeepSayingMarcoOverAndOver
            {
                ETag = sourceEtag
            });
//            VirtualClock.Current.AdvanceBy(TimeSpan.FromSeconds(2));
            firstPassEtags = scheduled.Select(c => c.ETag).ToArray();
            Console.WriteLine(new { firstPassEtags }.ToLogString());

            scheduled.Clear();

            // revert the aggregate and do the same thing again
            it = await itRepo.GetLatest(it.Id);
            await it.ApplyAsync(new MarcoPoloPlayerWhoIsIt.KeepSayingMarcoOverAndOver
            {
                ETag = sourceEtag
            });

            Console.WriteLine("about to advance clock for the second time");

//            VirtualClock.Current.AdvanceBy(TimeSpan.FromSeconds(2));
            secondPassEtags = scheduled.Select(c => c.ETag).ToArray();
            Console.WriteLine(new { secondPassEtags }.ToLogString());

            secondPassEtags.Should()
                           .Equal(firstPassEtags);
        }

        [Test]
        public async Task Aggregates_can_schedule_commands_against_themselves_idempotently()
        {
            var it = new MarcoPoloPlayerWhoIsIt();
            await itRepo.Save(it);

            await it.ApplyAsync(new MarcoPoloPlayerWhoIsIt.KeepSayingMarcoOverAndOver());

            VirtualClock.Current.AdvanceBy(TimeSpan.FromMinutes(1));

            await itRepo.Refresh(it);

            it.Events()
              .OfType<MarcoPoloPlayerWhoIsIt.SaidMarco>()
              .Count()
              .Should()
              .BeGreaterOrEqualTo(5);
        }

        public static Order CreateOrder(
            DateTimeOffset? deliveryBy = null,
            string customerName = null,
            Guid? orderId = null,
            Guid? customerAccountId = null)
        {
            return new Order(
                new CreateOrder(customerName ?? Any.FullName())
                {
                    AggregateId = orderId ?? Any.Guid(),
                    CustomerId = customerAccountId ?? Any.Guid()
                })
                .Apply(new AddItem
                {
                    Price = 499.99m,
                    ProductName = Any.Words(1, true).Single()
                })
                .Apply(new SpecifyShippingInfo
                {
                    Address = Any.Words(1, true).Single() + " St.",
                    City = "Seattle",
                    StateOrProvince = "WA",
                    Country = "USA",
                    DeliverBy = deliveryBy
                });
        }
    }
}