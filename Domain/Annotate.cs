// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    public class Annotate<TAggregate> : Command<TAggregate>
        where TAggregate : class
    {
        public string Message { get; set; }

        public Annotate(string message)
        {
            Message = message;
        }
    }
}