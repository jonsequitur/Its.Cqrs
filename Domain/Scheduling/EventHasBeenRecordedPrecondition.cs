// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    [DebuggerDisplay("{ToString()}")]
    public class EventHasBeenRecordedPrecondition : IPrecondition
    {
        private readonly string scope;

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

        [JsonConstructor]
        public EventHasBeenRecordedPrecondition(string etag, Guid aggregateId) : this(etag, aggregateId.ToString())
        {
            AggregateId = aggregateId;
        }

        public Guid AggregateId { get; private set; }

        public string ETag { get; }

        string IPrecondition.Scope => scope;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{scope.Substring(0, Math.Min(scope.Length, 4))}...{ETag}";
        }
    }
}