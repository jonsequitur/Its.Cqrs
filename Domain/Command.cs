// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public abstract class Command : ICommand
    {
        private IPrincipal principal;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// </summary>
        protected Command(string etag = null)
        {
            ETag = etag;
        }

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public virtual string CommandName
        {
            get
            {
                return GetType().Name;
            }
        }

        /// <summary>
        /// Gets the ETag for the command.
        /// </summary>
        /// <remarks>By defalut, this is set to a new Guid, to prevent the same command instance from ever being applied twice. This behavior can be overridden by setting this property to a different value.</remarks>
        public string ETag { get; set; }

        /// <summary>
        ///     Gets or sets the principal on whose behalf the command will be authorized.
        /// </summary>
        [JsonIgnore]
        public IPrincipal Principal
        {
            get
            {
                return principal ?? (principal = Thread.CurrentPrincipal);
            }
            set
            {
                principal = value;
            }
        }

        internal void AssignRandomETag()
        {
            if (!string.IsNullOrWhiteSpace(ETag))
            {
                throw new InvalidOperationException("ETag is already assigned.");
            }

            ETag = Guid.NewGuid().ToString("N").ToETag();
        }

        private static readonly Lazy<Dictionary<Tuple<Type, string>, Type>> index = new Lazy<Dictionary<Tuple<Type, string>, Type>>
            (() => AggregateType.KnownTypes
                                .Select(aggregateType =>
                                        new
                                        {
                                            aggregateType,
                                            commandTypes = (IEnumerable<Type>) typeof (Command<>).MakeGenericType(aggregateType)
                                                                                                 .Member()
                                                                                                 .KnownTypes
                                        })
                                .SelectMany(ts => ts.commandTypes
                                                    .Select(ct =>
                                                            new
                                                            {
                                                                key = Tuple.Create(ts.aggregateType, ct.Name),
                                                                value = ct
                                                            }))
                                .ToDictionary(p => p.key, p => p.value));

        /// <summary>
        /// Gets all known <see cref="Command" /> types.
        /// </summary>
        public static Type[] KnownTypes
        {
            get
            {
                return index.Value.Values.ToArray();
            }
        }

        internal static Type FindType(Type aggregateType, string commandName)
        {
            Type type;
            index.Value.TryGetValue(Tuple.Create(aggregateType, commandName), out type);
            return type;
        }
    }
}
