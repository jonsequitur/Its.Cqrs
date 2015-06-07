// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Gets an <see cref="IEventSourcedRepository{TAggregate}" />.
        /// </summary>
        public static IEventSourcedRepository<TAggregate> Repository<TAggregate>(this Configuration configuration)
            where TAggregate : class, IEventSourced
        {
            return configuration.Container.Resolve<IEventSourcedRepository<TAggregate>>();
        }

        /// <summary>
        /// Gets an <see cref="ICommandScheduler{TAggregate}" />.
        /// </summary>
        public static ICommandScheduler<TAggregate> CommandScheduler<TAggregate>(this Configuration configuration)
            where TAggregate : class, IEventSourced
        {
            return configuration.Container.Resolve<ICommandScheduler<TAggregate>>();
        }

        /// <summary>
        /// Configures the domain to use the specified event bus.
        /// </summary>
        public static Configuration UseEventBus(
            this Configuration configuration,
            IEventBus bus)
        {
            configuration.Container.RegisterSingle(c => bus);
            return configuration;
        }

        /// <summary>
        /// Enables Its.Domain to instantiate dependencies of command or event handlers.
        /// </summary>
        /// <typeparam name="T">The type of the dependency.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <param name="resolve">A delegate that will be called each time the specified type is needed by an object that Its.Domain is instantiating.</param>
        /// <returns></returns>
        /// <remarks>This method is called when Its.Domain instantiates an implementation of one of its interfaces (e.g. <see cref="ICommandHandler{TAggregate,TCommand}" />) that depends on a type that is owned by application code. The typical usage of this method is to wire up an IoC container that you've configured for your application.</remarks>
        public static Configuration UseDependency<T>(
            this Configuration configuration,
            Func<Func<Type, object>, T> resolve)
        {
            // ReSharper disable RedundantTypeArgumentsOfMethod
            configuration.Container.Register<T>(c => resolve(c.Resolve));
            // ReSharper restore RedundantTypeArgumentsOfMethod
            return configuration;
        }

        /// <summary>
        /// Enables Its.Domain to instantiate dependencies of command or event handlers.
        /// </summary>
        /// <remarks>
        ///  This method is called when Its.Domain instantiates an implementation of one of its interfaces (e.g. <see cref="ICommandHandler{TAggregate,TCommand}" />) that depends on a type that is owned by application code. The typical usage of this method is to wire up an IoC container that you've configured for your application.
        /// 
        /// Usage example:
        /// 
        /// <code>
        /// 
        /// MyContainer myContainer; 
        /// 
        /// configuration.UseDependencies(
        ///     type => {       
        ///        if (myContainer.IsRegistered(type)) 
        ///        {  
        ///             return () => myContainer.Resolve(type);
        ///        } 
        ///        
        ///        return null;
        /// 
        ///     });
        /// 
        /// </code>
        /// 
        /// </remarks>
        public static Configuration UseDependencies(
            this Configuration configuration,
            Func<Type, Func<object>> strategy)
        {
            configuration.Container
                         .AddStrategy(t =>
                                      {
                                          Func<object> resolveFunc = strategy(t);
                                          if (resolveFunc != null)
                                          {
                                              return container => resolveFunc();
                                          }
                                          return null;
                                      });
            return configuration;
        }

        internal static ISnapshotRepository SnapshotRepository(this Configuration configuration)
        {
            return configuration.Container.Resolve<ISnapshotRepository>();
        }

        /// <summary>
        /// Gets an <see cref="IReservationService"/>
        /// </summary>
        public static IReservationService ReservationService(this Configuration configuration)
        {
            return configuration.Container.Resolve<IReservationService>();
        }
    }
}
