// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Runtime.Remoting.Messaging;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public class ConfigurationContext : IDisposable
    {
        private readonly Configuration configuration;

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
                throw new ArgumentNullException("configuration");
            }
            this.configuration = configuration;
        }

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

        public static ConfigurationContext Current
        {
            get
            {
                return CallContext.LogicalGetData(callContextKey)
                                  .IfTypeIs<Guid>()
                                  .Then(id => contexts.IfContains(id))
                                  .ElseDefault();
            }
        }

        public Configuration Configuration
        {
            get
            {
                return configuration;
            }
        }

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
