// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Api.Tests.Infrastructure
{
    public static class AggregateExtensions
    {
        public static TAggregate SavedToEventStore<TAggregate>(this TAggregate aggregate)
            where TAggregate : EventSourcedAggregate<TAggregate>
        {
            var repository = Configuration.Current.Repository<TAggregate>();

            repository.Save(aggregate).Wait();

            return aggregate;
        }
    }
}