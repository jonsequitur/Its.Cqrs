// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    public class Annotate<TAggregate> : Command<TAggregate>
        where TAggregate : class
    {
        public Annotate(string message, string etag = null) : base(etag)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            Message = message;
        }

        public string Message { get; set; }
    }
}