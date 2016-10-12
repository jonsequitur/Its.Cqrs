// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A command that can be applied to an aggregate.
    /// </summary>
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
        public virtual string CommandName => GetType().Name;

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

        private static readonly Lazy<Dictionary<Tuple<Type, string>, Type>> indexOfCommandTypesByTargetTypeAndCommandName =
            new Lazy<Dictionary<Tuple<Type, string>, Type>>(BuildCommandTypeIndex);

        private static readonly Lazy<Dictionary<Type, Type>> indexOfTargetTypesByCommandType =
            new Lazy<Dictionary<Type, Type>>(() => indexOfCommandTypesByTargetTypeAndCommandName
                                                       .Value
                                                       .Distinct()
                                                       .ToDictionary(keySelector: p => p.Value,
                                                                     elementSelector: p => p.Key.Item1));

        private static readonly Lazy<Type[]> knownTargetTypes =
            new Lazy<Type[]>(() =>
                             indexOfCommandTypesByTargetTypeAndCommandName
                                 .Value
                                 .Keys
                                 .Select(key => key.Item1)
                                 .Distinct()
                                 .ToArray());

        private static Dictionary<Tuple<Type, string>, Type> BuildCommandTypeIndex()
        {
            return Discover.ConcreteTypesDerivedFrom(typeof (ICommand))
                           .Select(commandType => new
                           {
                               commandType,
                               targetType = commandType.GetInterface("ICommand`1")
                                                       .IfNotNull()
                                                       .Then(i => i.GetGenericArguments().Single())
                                                       .ElseDefault()
                           })
                           .ToDictionary(
                               p => Tuple.Create(p.targetType, p.commandType.Name),
                               p => p.commandType);
        }

        /// <summary>
        /// Gets all known <see cref="Command" /> types.
        /// </summary>
        public static Type[] KnownTypes =>
            indexOfCommandTypesByTargetTypeAndCommandName
                .Value
                .Values
                .ToArray();

        /// <summary>
        /// Gets the known types within the <see cref="AppDomain" /> that commands can be applied to.
        /// </summary>
        public static Type[] KnownTargetTypes => knownTargetTypes.Value;

        internal static Type FindType(Type aggregateType, string commandName)
        {
            Type type;
            indexOfCommandTypesByTargetTypeAndCommandName.Value.TryGetValue(Tuple.Create(aggregateType, commandName), out type);
            return type;
        }

        internal static string TargetNameFor(Type commandType) => indexOfTargetTypesByCommandType.Value[commandType].Name;
    }
}