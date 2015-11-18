// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    [Serializable]
    public class PreconditionNotMetException : InvalidOperationException
    {
        public PreconditionNotMetException(CommandPrecondition precondition)
            : base("Precondition was not met: " + precondition)
        {
            if (precondition == null)
            {
                throw new ArgumentNullException("precondition");
            }
            Precondition = precondition;
        }

        public PreconditionNotMetException(
            string message,
            Guid aggregateId,
            string etag = null) : base(message)
        {
            Precondition = new CommandPrecondition
            {
                AggregateId = aggregateId,
                ETag = etag
            };
        }

        public CommandPrecondition Precondition { get; private set; }
    }
}