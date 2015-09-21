// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using Microsoft.Its.Recipes;
using Pocket;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Represents the configuration of the basic services for the domain, such as command scheduling, event aggregation, and dependency resolution.
    /// </summary>
    public class Configuration : IDisposable
    {
        private static readonly Configuration global;
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly PocketContainer container = new PocketContainer
                                                     {
                                                         OnFailedResolve =
                                                             (type, exception) =>
                                                             new DomainConfigurationException(
                                                             string.Format(
                                                                 "Its.Domain can't create an instance of {0} unless you register it first via Configuration.UseDependency or Configuration.UseDependencies.",
                                                                 type), exception)
                                                     };

        private readonly ConcurrentDictionary<string, object> properties = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Initializes the <see cref="Configuration"/> class.
        /// </summary>
        static Configuration()
        {
            global = new Configuration();
            global.Container.Register<IEventBus>(c => InProcessEventBus.Instance);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration()
        {
            container.AvoidConstructorsWithPrimitiveTypes()
                     .IfOnlyOneImplementationUseIt()
                     .UseImmediateCommandScheduling()
                     .RegisterSingle<IReservationService>(c => new NoReservations())
                     .RegisterSingle<IEventBus>(c => new InProcessEventBus())
                     .Register<ISnapshotRepository>(c => new NoSnapshots())
                     .Register<ICommandPreconditionVerifier>(c => new NoCommandPreconditionVerifications())
                     .RegisterSingle(c => this);
        }

        [Obsolete("Please use Configuration.Current instead.")]
        public static Configuration Global
        {
            get
            {
                return global;
            }
        }

        /// <summary>
        /// Gets the current configuration. This may vary by context.
        /// </summary>
        public static Configuration Current
        {
            get
            {
                return ConfigurationContext.Current
                                           .IfNotNull()
                                           .Then(context => context.Configuration)
                                           .Else(() => global);
            }
        }

        /// <summary>
        /// Gets or sets the reservation service, if configured.
        /// </summary>
        public IReservationService ReservationService
        {
            get
            {
                return container.Resolve<IReservationService>() ?? NoReservations.Instance;
            }
            set
            {
                var reservationService = value ?? NoReservations.Instance;
                container.RegisterSingle(c => reservationService);
            }
        }

        /// <summary>
        /// Gets the event bus.
        /// </summary>
        public IEventBus EventBus
        {
            get
            {
                return container.Resolve<IEventBus>();
            }
        }

        /// <summary>
        /// Registers an object for disposal when the configuration is disposed.
        /// </summary>
        /// <param name="disposable">The object to dispose.</param>
        public void RegisterForDisposal(IDisposable disposable)
        {
            disposables.Add(disposable);
        }

        internal PocketContainer Container
        {
            get
            {
                return container;
            }
        }

        internal ConcurrentDictionary<string, object> Properties
        {
            get
            {
                return properties;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
