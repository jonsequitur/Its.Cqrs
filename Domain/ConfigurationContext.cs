// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Sets up an ambient context in which a given domain configuration is used.
    /// </summary>
    [DebuggerStepThrough]
    public class ConfigurationContext : IDisposable
    {
        private static readonly string callContextKey = typeof (ConfigurationContext).FullName;

        private static readonly ConcurrentDictionary<Guid, ConfigurationContext> contexts = new ConcurrentDictionary<Guid, ConfigurationContext>();

        private readonly Guid Id = Guid.NewGuid();

        /// <summary>
        /// Prevents a default instance of the <see cref="ConfigurationContext"/> class from being created.
        /// </summary>
        private ConfigurationContext(Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            Configuration = configuration;
        }

        /// <summary>
        /// Sets <see cref="Configuration.Current" /> to the specified configuration for all callers on the current <see cref="CallContext" />.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>A <see cref="ConfigurationContext" /> that, when disposed, reverts the configuration to the previous (possibly global) one.</returns>
        /// <exception cref="System.InvalidOperationException">ConfigurationContexts cannot be nested.</exception>
        public static ConfigurationContext Establish(Configuration configuration)
        {
            var current = Current;

            if (current != null && !current.AllowOverride)
            {
                throw new InvalidOperationException("ConfigurationContexts cannot be nested.");
            }

            current = new ConfigurationContext(configuration);
            CallContext.LogicalSetData(callContextKey, current.Id);
            contexts.GetOrAdd(current.Id, current);

            return current;
        }

        internal bool AllowOverride { get; set; }

        /// <summary>
        /// Gets the current configuration context, or null.
        /// </summary>
        public static ConfigurationContext Current =>
            CallContext.LogicalGetData(callContextKey)
                       .IfTypeIs<Guid>()
                       .Then(id => contexts.IfContains(id))
                       .ElseDefault();

        /// <summary>
        /// Gets the configuration used within the current context.
        /// </summary>
        public Configuration Configuration { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            ConfigurationContext configurationContext;
            contexts.TryRemove(Id, out configurationContext);
            CallContext.FreeNamedDataSlot(callContextKey);
        }
    }
}
