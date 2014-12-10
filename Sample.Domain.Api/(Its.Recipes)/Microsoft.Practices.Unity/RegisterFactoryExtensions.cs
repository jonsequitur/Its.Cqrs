// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Practices.Unity
{
    /// <summary>
    ///     Facilitates the use of lambda expressions to register types in Unity.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static partial class RegisterFactoryExtensions
    {
        /// <summary>
        ///     Registers a factory method that returns the specified <see cref="Type" />
        /// </summary>
        /// <typeparam name="TFrom">
        ///     The <see cref="Type" /> of the requested service.
        /// </typeparam>
        /// <param name="container"> The container. </param>
        /// <param name="factory"> The factory method. </param>
        /// <returns>
        ///     An instance of <see cref="Type" /> <typeparamref name="TFrom" /> .
        /// </returns>
        public static IUnityContainer RegisterFactory<TFrom>(this IUnityContainer container,
                                                             Func<IUnityContainer, TFrom> factory)
        {
            return container.RegisterFactory<TFrom>(null, factory);
        }

        /// <summary>
        ///     Registers a factory method that returns the specified <see cref="Type" />
        /// </summary>
        /// <typeparam name="TFrom">
        ///     The <see cref="Type" /> of the requested service.
        /// </typeparam>
        /// <param name="container"> The container. </param>
        /// <param name="factory"> The factory method. </param>
        /// <param name="name">
        ///     The key to a keyed instance, for when there are potentially multiple instances of the same
        ///     <see
        ///         cref="Type" />
        ///     registered.
        /// </param>
        /// <returns>
        ///     An instance of <see cref="Type" /> <typeparamref name="TFrom" /> .
        /// </returns>
        public static IUnityContainer RegisterFactory<TFrom>(this IUnityContainer container, string name,
                                                             Func<IUnityContainer, TFrom> factory)
        {
            container.RegisterType<TFrom>(name, new InjectionFactory(c => factory(c)));
            return container;
        }

        /// <summary>
        ///     Registers a factory method that returns the specified <see cref="Type" />. The factory is only called once during the lifetime of the container, and the result is returned again on each subsequent request for the registered
        ///     <typeparamref
        ///         name="TFrom" />
        ///     .
        /// </summary>
        /// <typeparam name="TFrom">
        ///     The <see cref="Type" /> of the requested service.
        /// </typeparam>
        /// <param name="container"> The container. </param>
        /// <param name="factory"> The factory method. </param>
        /// <param name="lifetimeManager"> The lifetime manager to be used with this registration. </param>
        /// <returns>
        ///     An instance of <see cref="Type" /> <typeparamref name="TFrom" /> .
        /// </returns>
        public static IUnityContainer RegisterFactory<TFrom>(this IUnityContainer container,
                                                             Func<IUnityContainer, TFrom> factory,
                                                             LifetimeManager lifetimeManager)
        {
            return container.RegisterFactory<TFrom>(null, factory, lifetimeManager);
        }

        /// <summary>
        ///     Registers a factory method that returns the specified <see cref="Type" />. The factory is only called once during the lifetime of the container, and the result is returned again on each subsequent request for the registered
        ///     <typeparamref
        ///         name="TFrom" />
        ///     .
        /// </summary>
        /// <typeparam name="TFrom">
        ///     The <see cref="Type" /> of the requested service.
        /// </typeparam>
        /// <param name="container"> The container. </param>
        /// <param name="name">
        ///     The key to a keyed instance, for when there are potentially multiple instances of the same
        ///     <see
        ///         cref="Type" />
        ///     registered.
        /// </param>
        /// <param name="factory"> The factory method. </param>
        /// <param name="lifetimeManager"> The lifetime manager to be used for this registration. </param>
        /// <returns>
        ///     An instance of <see cref="Type" /> <typeparamref name="TFrom" /> .
        /// </returns>
        public static IUnityContainer RegisterFactory<TFrom>(this IUnityContainer container, string name,
                                                             Func<IUnityContainer, TFrom> factory,
                                                             LifetimeManager lifetimeManager)
        {
            container.RegisterType<TFrom>(name, lifetimeManager,
                                          new InjectionFactory(c => factory(c)));
            return container;
        }
    }
}