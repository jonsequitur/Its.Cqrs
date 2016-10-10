// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A precondition based on the presence of an event in the event store.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.IPrecondition" />
    [DebuggerDisplay("{ToString()}")]
    public class EventHasBeenRecordedPrecondition : IPrecondition
    {
        private readonly string scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHasBeenRecordedPrecondition"/> class.
        /// </summary>
        /// <param name="etag">The etag.</param>
        /// <param name="scope">The scope.</param>
        /// <exception cref="System.ArgumentException">
        /// etag cannot be null, empty, or whitespace.
        /// or
        /// scope cannot be null, empty, or whitespace.
        /// </exception>
        public EventHasBeenRecordedPrecondition(string etag, string scope)
        {
            if (string.IsNullOrWhiteSpace(etag))
            {
                throw new ArgumentException("etag cannot be null, empty, or whitespace.");
            }
            if (string.IsNullOrWhiteSpace(scope))
            {
                throw new ArgumentException("scope cannot be null, empty, or whitespace.");
            }
            this.scope = scope;
            ETag = etag;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHasBeenRecordedPrecondition"/> class.
        /// </summary>
        /// <param name="etag">The etag.</param>
        /// <param name="aggregateId">The aggregate identifier.</param>
        [JsonConstructor]
        public EventHasBeenRecordedPrecondition(string etag, Guid aggregateId) : this(etag, aggregateId.ToString())
        {
            AggregateId = aggregateId;
        }

        /// <summary>
        /// Gets the aggregate id of the event.
        /// </summary>
        public Guid AggregateId { get; private set; }

        /// <summary>
        /// Gets the etag, which must be unique within the precondition's <see cref="IPrecondition.Scope" />.
        /// </summary>
        public string ETag { get; }

        string IPrecondition.Scope => scope;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{scope.Substring(0, Math.Min(scope.Length, 4))}...{ETag}";
    }
}