// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Helps to organize sets of events by aggregate.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    public class AggregateBuilder<TAggregate> where TAggregate : IEventSourced
    {
        private readonly Guid aggregateId;
        private readonly ScenarioBuilder scenarioBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateBuilder{TAggregate}"/> class.
        /// </summary>
        /// <param name="aggregateId">The aggregate id to be used for this aggregate.</param>
        /// <param name="scenarioBuilder">The associated scenario builder.</param>
        /// <exception cref="System.ArgumentNullException">scenarioBuilder</exception>
        public AggregateBuilder(Guid aggregateId, ScenarioBuilder scenarioBuilder)
        {
            if (scenarioBuilder == null)
            {
                throw new ArgumentNullException("scenarioBuilder");
            }
            this.aggregateId = aggregateId;
            this.scenarioBuilder = scenarioBuilder;
        }

        /// <summary>
        /// Gets the aggregate identifier that will be used for all events added to the scenario via the <see cref="AggregateBuilder{T}" /> instance.
        /// </summary>
        public Guid AggregateId
        {
            get
            {
                return aggregateId;
            }
        }

        /// <summary>
        /// Gets the initial events of the scenario.
        /// </summary>
        /// <remarks>Events added when saving aggregates via a repository after Prepare is called will not be added to <see cref="InitialEvents" />.</remarks>
        public IEnumerable<IEvent<TAggregate>> InitialEvents
        {
            get
            {
                return scenarioBuilder
                    .InitialEvents
                    .Where(e => e.AggregateId == aggregateId)
                    .Cast<IEvent<TAggregate>>();
            }
        }

        /// <summary>
        /// Adds events for the specified aggregate.
        /// </summary>
        public virtual AggregateBuilder<TAggregate> AddEvents(params IEvent<TAggregate>[] events)
        {
            foreach (var @event in events)
            {
                ((dynamic) @event).AggregateId = aggregateId;
                scenarioBuilder.AddEvents(@event);
            }
            return this;
        }
    }
}