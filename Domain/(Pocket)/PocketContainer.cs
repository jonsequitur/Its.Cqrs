// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// It has been imported using NuGet from the PocketContainer project (https://github.com/jonsequitur/PocketContainer). 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Pocket
{
    /// <summary>
    /// An embedded dependency injection container, for when you want to use a container without adding an assembly dependency.
    /// </summary>
    /// <remarks>The default resolution strategy follows Unity's conventions. A concrete type can be resolved without explicit registration. It will choose the longest constructor and resolve the types to satisfy its arguments. This continues recursively until the graph is built or it fails to build a dependency.</remarks>
#if !SourceProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal partial class PocketContainer : IEnumerable<KeyValuePair<Type, Func<PocketContainer, object>>>
    {
        private ConcurrentDictionary<Type, Func<PocketContainer, object>> resolvers = new ConcurrentDictionary<Type, Func<PocketContainer, object>>();
        private static readonly MethodInfo resolveMethod = typeof (PocketContainer).GetMethod("Resolve", new Type[0]);
        private readonly ConcurrentDictionary<Type, dynamic> singletons = new ConcurrentDictionary<Type, dynamic>();
        private Func<Type, Func<PocketContainer, object>> strategyChain = type => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PocketContainer"/> class.
        /// </summary>
        public PocketContainer()
        {
            Register(c => this);

            AddStrategy(type =>
            {
                // add a default strategy for Func<T> to resolve by convention to return a Func that does a resolve when invoked
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Func<>))
                {
                    var funcReturnType = type.GetGenericArguments().Single();

                    var func = (Func<PocketContainer, object>)
                               GetType()
                                   .GetMethod("MakeResolverFunc", BindingFlags.Instance | BindingFlags.NonPublic)
                                   .MakeGenericMethod(funcReturnType)
                                   .Invoke(this, null);

                    return func;
                }

                return null;
            });
        }

        /// <summary>
        /// Resolves an instance of the specified type.
        /// </summary>
        public virtual T Resolve<T>()
        {
            return (T) resolvers.GetOrAdd(typeof (T), t =>
            {
                var customFactory = strategyChain(t);
                if (customFactory != null)
                {
                    return customFactory;
                }

                Func<PocketContainer, T> defaultFactory;
                try
                {
                    defaultFactory = Factory<T>.Default;
                }
                catch (TypeInitializationException ex)
                {
                    throw OnFailedResolve(typeof(T), ex);
                }

                return c => defaultFactory(c);
            })(this);
        }

        /// <summary>
        /// Returns an exception to be thrown when resolve fails.
        /// </summary>
        public Func<Type, Exception, Exception> OnFailedResolve = (type, exception) =>
            new ArgumentException(string.Format("PocketContainer can't construct a {0} unless you register it first. ☹", type), exception);

        /// <summary>
        /// Resolves an instance of the specified type.
        /// </summary>
        public object Resolve(Type type)
        {
            Func<PocketContainer, object> func;
            if (!resolvers.TryGetValue(type, out func))
            {
                return resolveMethod.MakeGenericMethod(type).Invoke(this, null);
            }
            return func(this);
        }

        /// <remarks>When an unregistered type is resolved for the first time, the strategies are checked until one returns a delegate. This delegate will be used in the future to resolve the specified type.</remarks>
        public PocketContainer AddStrategy(
            Func<Type, Func<PocketContainer, object>> strategy,
            bool executeFirst = true)
        {
            var previousStrategy = strategyChain;
            if (executeFirst)
            {
                strategyChain = type => strategy(type) ?? previousStrategy(type);
            }
            else
            {
                strategyChain = type => previousStrategy(type) ?? strategy(type);
            }
            return this;
        }

        /// <summary>
        /// Registers a delegate to retrieve instances of the specified type.
        /// </summary>
        public PocketContainer Register<T>(Func<PocketContainer, T> factory)
        {
            resolvers[typeof (T)] = c => factory(c);
            resolvers[typeof (Lazy<T>)] = c => new Lazy<T>(c.Resolve<T>);
            return this;
        }

        /// <summary>
        /// Registers a delegate to retrieve instances of the specified type.
        /// </summary>
        public PocketContainer Register(Type type, Func<PocketContainer, dynamic> factory)
        {
            typeof (PocketContainer).GetMethods()
                                    .Single(m => m.Name == "Register" && m.IsGenericMethod)
                                    .MakeGenericMethod(type)
                                    .Invoke(this, new object[] { ConvertFunc(factory, type) });
            return this;
        }

        /// <summary>
        /// Registers a delegate to retrieve an instance of the specified type when it is first resolved. This instance will be reused for the lifetime of the container.
        /// </summary>
        public PocketContainer RegisterSingle<T>(Func<PocketContainer, T> factory)
        {
            Register<T>(c => singletons.GetOrAdd(typeof (T), t => factory(c)));
            dynamic _;
            singletons.TryRemove(typeof (T), out _);
            return this;
        }

        /// <summary>
        /// Registers a delegate to retrieve an instance of the specified type when it is first resolved. This instance will be reused for the lifetime of the container.
        /// </summary>
        public PocketContainer RegisterSingle(Type type, Func<PocketContainer, dynamic> factory)
        {
            typeof (PocketContainer).GetMethods()
                                    .Single(m => m.Name == "RegisterSingle" && m.IsGenericMethod)
                                    .MakeGenericMethod(type)
                                    .Invoke(this, new object[] { ConvertFunc(factory, type) });
            return this;
        }

        private Delegate ConvertFunc(Func<PocketContainer, dynamic> func, Type resultType)
        {
            var containerParam = Expression.Parameter(typeof (PocketContainer), "c");

            ConstantExpression constantExpression = null;
            if (func.Target != null)
            {
                constantExpression = Expression.Constant(func.Target);
            }

// ReSharper disable PossiblyMistakenUseOfParamsMethod
            var call = Expression.Call(constantExpression, func.Method, containerParam);
// ReSharper restore PossiblyMistakenUseOfParamsMethod
            var delegateType = typeof (Func<,>).MakeGenericType(typeof (PocketContainer), resultType);
            var body = Expression.Convert(call, resultType);
            var expression = Expression.Lambda(delegateType,
                                               body, 
                                               containerParam);
            return expression.Compile();
        }

        internal static class Factory<T>
        {
            public static readonly Func<PocketContainer, T> Default = Build.UsingLongestConstructor<T>();
        }

        internal static class Build
        {
            public static Func<PocketContainer, T> UsingLongestConstructor<T>()
            {
                if (typeof(Delegate).IsAssignableFrom(typeof(T)))
                {
                    throw new TypeInitializationException(typeof(T).FullName, null);
                }

                var ctors = typeof (T).GetConstructors();

                var longestCtorParamCount = ctors.Max(c => c.GetParameters().Count());

                var chosenCtor = ctors.Single(c => c.GetParameters().Length == longestCtorParamCount);

                var container = Expression.Parameter(typeof (PocketContainer), "container");

                var factoryExpr = Expression.Lambda<Func<PocketContainer, T>>(
                    Expression.New(chosenCtor,
                                   chosenCtor.GetParameters()
                                             .Select(p =>
                                                     Expression.Call(container,
                                                                     resolveMethod
                                                                         .MakeGenericMethod(p.ParameterType)))),
                    container);

                return factoryExpr.Compile();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<Type, Func<PocketContainer, object>>> GetEnumerator()
        {
            return resolvers.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

// ReSharper disable UnusedMember.Local
        private Func<PocketContainer, Func<T>> MakeResolverFunc<T>()
// ReSharper restore UnusedMember.Local
        {
            var container = Expression.Parameter(typeof (PocketContainer), "container");

            var resolve = Expression.Lambda<Func<PocketContainer, Func<T>>>(
                Expression.Lambda<Func<T>>(
                    Expression.Call(container,
                                    resolveMethod.MakeGenericMethod(typeof (T)))),
                container);

            return resolve.Compile();
        }
    }
}