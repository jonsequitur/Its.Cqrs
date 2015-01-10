// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A command that is used to create new instances of the target type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The aggregate type to which the command applies.</typeparam>
    [DebuggerStepThrough]
    public abstract class ConstructorCommand<T> : Command<T> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorCommand{T}"/> class.
        /// </summary>
        protected ConstructorCommand()
        {
            AggregateId = Guid.NewGuid();
        }

        /// <summary>
        /// Gets or sets the aggregate identifier to be used for the new instance of <typeparamref name="T" />.
        /// </summary>
        /// <value>
        /// The aggregate identifier.
        /// </value>
        public Guid AggregateId { get; set; }
   
        /// <summary>
        /// If set, requires that the command be applied to this version of the aggregate; otherwise, <see cref="Command{TAggregate}.ApplyTo" /> will throw..
        /// </summary>
        /// <remarks>For <see cref="ConstructorCommand{T}" />, this value is always 0 and cannot be set to a different value.</remarks>
        [JsonIgnore]
        public override long? AppliesToVersion
        {
            get
            {
                return 0;
            }
            set
            {
                throw new InvalidOperationException("ConstructorCommand<T> can only be applied at version 0 of an aggregate.");
            }
        }
    }
}
