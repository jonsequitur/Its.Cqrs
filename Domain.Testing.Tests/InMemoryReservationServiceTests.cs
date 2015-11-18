// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Tests;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class InMemoryReservationServiceTests : ReservationServiceTests
    {
        private InMemoryEventStream eventStream;

        protected override void Configure(Configuration configuration, Action onSave = null)
        {
            configuration.UseInMemoryReservationService()
                .UseInMemoryEventStore()
                .UseEventBus(new FakeEventBus())
                .UseDependency(_ => eventStream);
        }

        protected override IEventSourcedRepository<TAggregate> CreateRepository<TAggregate>(
            Action onSave = null)
        {
            if (onSave != null)
            {
                eventStream.BeforeSave += (sender, @event) => onSave();
            }

            return Configuration.Current.Repository<TAggregate>();
        }
    }
}