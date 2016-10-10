// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Annotates the target aggregate by inserting an <see cref="Annotated{TAggregate}" /> into its event stream.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <seealso cref="Microsoft.Its.Domain.Command{TAggregate}" />
    public class Annotate<TAggregate> : Command<TAggregate>
        where TAggregate : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Annotate{TAggregate}"/> class.
        /// </summary>
        /// <param name="message">The message to be recorded with the annotation event.</param>
        /// <param name="etag">The etag for the command.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Annotate(string message, string etag = null) : base(etag)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            Message = message;
        }

        /// <summary>
        /// Gets or sets the message to be recorded with the annotation event.
        /// </summary>
        public string Message { get; set; }
    }
}