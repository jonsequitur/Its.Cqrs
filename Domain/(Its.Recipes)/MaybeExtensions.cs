// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;

namespace Microsoft.Its.Recipes
{
    /// <summary>
    ///     Supports chaining of expressions when intermediate values may be null, to support a fluent API style using common .NET types.
    /// </summary>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class MaybeExtensions
    {
        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="otherValue">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="otherValue" />.
        /// </returns>
        public static T Else<T>(this Maybe<T> maybe, Func<T> otherValue)
        {
            if (maybe.HasValue)
            {
                return maybe.Value;
            }

            return otherValue();
        }

        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="other">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="other" />.
        /// </returns>
        public static Maybe<T> Else<T>(this Maybe<T> maybe, Func<Maybe<T>> other)
        {
            return maybe.HasValue
                       ? maybe
                       : other();
        }

        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="otherValue">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="otherValue" />.
        /// </returns>
        public static T Else<T>(this Maybe<Maybe<T>> maybe, Func<T> otherValue)
        {
            if (maybe.HasValue)
            {
                return maybe.Value.Else(otherValue);
            }

            return otherValue();
        }

        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="otherValue">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="otherValue" />.
        /// </returns>
        public static T Else<T>(this Maybe<Maybe<Maybe<T>>> maybe, Func<T> otherValue)
        {
            if (maybe.HasValue)
            {
                return maybe.Value.Else(otherValue);
            }

            return otherValue();
        }

        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="otherValue">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="otherValue" />.
        /// </returns>
        public static T Else<T>(this Maybe<Maybe<Maybe<Maybe<T>>>> maybe, Func<T> otherValue)
        {
            if (maybe.HasValue)
            {
                return maybe.Value.Else(otherValue);
            }

            return otherValue();
        }

        /// <summary>
        /// Returns the default value for <typeparamref name="T" /> if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static T ElseDefault<T>(this Maybe<T> maybe)
        {
            return maybe.Else(() => default(T));
        }

        /// <summary>
        /// Returns the default value for <typeparamref name="T" /> if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static T ElseDefault<T>(this Maybe<Maybe<T>> maybe)
        {
            return maybe.Else(() => default(T));
        }

        /// <summary>
        /// Returns the default value for <typeparamref name="T" /> if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static T ElseDefault<T>(this Maybe<Maybe<Maybe<T>>> maybe)
        {
            return maybe.Else(() => default(T));
        }

        /// <summary>
        /// Returns the default value for <typeparamref name="T" /> if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static T ElseDefault<T>(this Maybe<Maybe<Maybe<Maybe<T>>>> maybe)
        {
            return maybe.Else(() => default(T));
        }

        /// <summary>
        /// Returns null if the source has no value.
        /// </summary>
        /// <typeparam name="T">The type held by the <see cref="Recipes.Maybe{T}" />.</typeparam>
        public static T? ElseNull<T>(this Maybe<T> maybe)
            where T : struct
        {
            if (maybe.HasValue)
            {
                return maybe.Value;
            }

            return null;
        }

        /// <summary>
        /// Performs an action if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static void ElseDo<T>(this Maybe<T> maybe, Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (!maybe.HasValue)
            {
                action();
            }
        }

        /// <summary>
        /// Throws an exception if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">The type held by the <see cref="Recipes.Maybe{T}" />.</typeparam>
        /// <param name="maybe">The maybe.</param>
        /// <param name="exception">A function that returns the exception to be thrown.</param>
        /// <returns></returns>
        public static T ElseThrow<T>(this Maybe<T> maybe, Func<Exception> exception)
        {
            if (maybe.HasValue)
            {
                return maybe.Value;
            }

            throw exception();
        }

        /// <summary>
        ///     If the dictionary contains a value for a specified key, executes an action passing the corresponding value.
        /// </summary>
        /// <typeparam name="TKey"> The type of the key. </typeparam>
        /// <typeparam name="TValue"> The type of the value. </typeparam>
        /// <param name="dictionary"> The dictionary. </param>
        /// <param name="key"> The key. </param>
        /// <exception cref="ArgumentNullException">dictionary</exception>
        public static Maybe<TValue> IfContains<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            TValue value;
            if (dictionary != null && dictionary.TryGetValue(key, out value))
            {
                return Maybe<TValue>.Yes(value);
            }

            return Maybe<TValue>.No();
        }

        /// <summary>
        /// Allows two maybes to be combined so that the resulting maybe has its value transformed by the second if and only if the first has a value.
        /// </summary>
        /// <typeparam name="T1">The type of the <see cref="Maybe{T}" />.</typeparam>
        /// <param name="first">The first maybe.</param>
        /// <returns></returns>
        public static T1 And<T1>(
            this Maybe<T1> first)
        {
            if (first.HasValue)
            {
                return first.Value;
            }

            return default(T1);
        }

        /// <summary>
        /// Attempts to retrieve a value dynamically.
        /// </summary>
        /// <typeparam name="T">The type of the value expected to be returned.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="getValue">A delegate that attempts to return a value via a dynamic invocation on the source object.</param>
        /// <remarks>This method will not cast the result value to <typeparamref name="T" />. If the returned value is not of this type, then a negative <see cref="Recipes.Maybe{T}" /> will be returned.</remarks>
        public static Maybe<T> IfHas<T>(
            this object source,
            Func<dynamic, T> getValue)
        {
            try
            {
                var value = getValue(source);
                return value.IfTypeIs<T>();
            }
            catch (RuntimeBinderException)
            {
                return Maybe<T>.No();
            }
        }

        /// <summary>
        /// Creates a <see cref="Recipes.Maybe{T}" /> that has a value if <paramref name="source" /> is not null. 
        /// </summary>
        /// <typeparam name="T">The type of the instance wrapped by the <see cref="Recipes.Maybe{T}" />.</typeparam>
        /// <param name="source">The source instance, which may be null.</param>
        public static Maybe<T> IfNotNull<T>(this T source) where T : class
        {
            if (source != null)
            {
                return Maybe<T>.Yes(source);
            }

            return Maybe<T>.No();
        }

        /// <summary>
        /// Creates a <see cref="Recipes.Maybe{T}" /> that has a value if <paramref name="source" /> has a value. 
        /// </summary>
        public static Maybe<T> IfNotNull<T>(this Maybe<T> source) where T : class
        {
            if (source.HasValue && source.Value != null)
            {
                return source;
            }

            return Maybe<T>.No();
        }

        /// <summary>
        /// Creates a <see cref="Recipes.Maybe{T}" /> that has a value if <paramref name="source" /> is not null. 
        /// </summary>
        /// <typeparam name="T">The type of the instance wrapped by the <see cref="Recipes.Maybe{T}" />.</typeparam>
        /// <param name="source">The source instance, which may be null.</param>
        public static Maybe<T> IfNotNull<T>(this T? source)
            where T : struct
        {
            if (source.HasValue)
            {
                return Maybe<T>.Yes(source.Value);
            }

            return Maybe<T>.No();
        }

        /// <summary>
        /// Creates a <see cref="Recipes.Maybe{T}" /> that has a value if <paramref name="source" /> is not null, empty, or entirely whitespace. 
        /// </summary>
        /// <param name="source">The string.</param>
        public static Maybe<string> IfNotNullOrEmptyOrWhitespace(this string source)
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                return Maybe<string>.Yes(source);
            }

            return Maybe<string>.No();
        }

        /// <summary>
        /// Creates a <see cref="Recipes.Maybe{T}" /> that has a value if <paramref name="source" /> is assignable to type <typeparamref name="T" />. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static Maybe<T> IfTypeIs<T>(
            this object source)
        {
            if (source is T)
            {
                return Maybe<T>.Yes((T) source);
            }

            return Maybe<T>.No();
        }

        /// <summary>
        ///     Returns either the <paramref name="source" /> or, if it is null, an empty <see cref="IEnumerable{T}" /> sequence.
        /// </summary>
        /// <typeparam name="T"> The type of the objects in the sequence. </typeparam>
        /// <param name="source"> The source sequence. </param>
        /// <returns> The source sequence or, if it is null, an empty sequence. </returns>
        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Attempts to get the value of a Try* method with an out parameter, for example <see cref="Dictionary{TKey,TValue}.TryGetValue" /> or <see cref="ConcurrentQueue{T}.TryDequeue" />.
        /// </summary>
        /// <typeparam name="T">The type of the source object.</typeparam>
        /// <typeparam name="TOut">The type the out parameter.</typeparam>
        /// <param name="source">The source object exposing the Try* method.</param>
        /// <param name="tryTryGetValue">A delegate to call the Try* method.</param>
        /// <returns></returns>
        public static Maybe<TOut> Out<T, TOut>(this T source, TryGetOutParameter<T, TOut> tryTryGetValue)
        {
            TOut result;

            if (tryTryGetValue(source, out result))
            {
                return Maybe<TOut>.Yes(result);
            }

            return Maybe<TOut>.No();
        }
     
        /// <summary>
        /// Specifies the result of a <see cref="Recipes.Maybe{T}" /> if the <see cref="Recipes.Maybe{T}" /> has a value.
        /// </summary>
        /// <typeparam name="TIn">The type of source object.</typeparam>
        /// <typeparam name="TOut">The type of result.</typeparam>
        /// <param name="maybe">The maybe.</param>
        /// <param name="getValue">A delegate to get the value from the source object.</param>
        public static Maybe<TOut> Then<TIn, TOut>(
            this Maybe<TIn> maybe,
            Func<TIn, TOut> getValue)
        {
            TOut value;
            return maybe.HasValue && (value = getValue(maybe.Value)) != null
                       ? Maybe<TOut>.Yes(value)
                       : Maybe<TOut>.No();
        }

        /// <summary>
        /// Performs an action if the <see cref="Recipes.Maybe{T}" /> has a value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static Maybe<Unit> ThenDo<T>(this Maybe<T> maybe, Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (maybe.HasValue)
            {
                action(maybe.Value);
                return Maybe<Unit>.Yes(Unit.Default);
            }

            return Maybe<Unit>.No();
        }

        /// <summary>
        /// Tries to call the specified method and catches exceptions if they occur.
        /// </summary>
        /// <typeparam name="TIn">The type of source object.</typeparam>
        /// <typeparam name="TOut">The type of result.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="getValue">A delegate to get the value from the source object.</param>
        /// <param name="ignore">A predicate to determine whether the exception should be ignored. If this is not specified, all exceptions are ignored. If it is specified and an exception is thrown that matches the predicate, the exception is ignored and a <see cref="Recipes.Maybe{TOut}" /> having no value is returned. If it is specified and an exception is thrown that does not match the predicate, the exception is allowed to propagate.</param>
        /// <returns></returns>
        public static Maybe<TOut> Try<TIn, TOut>(
            this TIn source,
            Func<TIn, TOut> getValue,
            Func<Exception, bool> ignore)
        {
            if (getValue == null)
            {
                throw new ArgumentNullException("getValue");
            }
            if (ignore == null)
            {
                throw new ArgumentNullException("ignore");
            }

            try
            {
                return Maybe<TOut>.Yes(getValue(source));
            }
            catch (Exception ex)
            {
                if (!ignore(ex))
                {
                    throw;
                }
            }

            return Maybe<TOut>.No();
        }
    }

    /// <summary>
    /// Represents an object that may or may not contain a value, allowing optional chained results to be specified for both possibilities.
    /// </summary>
    /// <typeparam name="T">The type of the possible value.</typeparam>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal struct Maybe<T>
    {
        private static readonly Maybe<T> no = new Maybe<T>
        {
            HasValue = false
        };

        private T value;

        /// <summary>
        /// Returns a <see cref="Recipes.Maybe{T}" /> that contains a value.
        /// </summary>
        /// <param name="value">The value.</param>
        public static Maybe<T> Yes(T value)
        {
            return new Maybe<T>
            {
                HasValue = true,
                value = value
            };
        }

        /// <summary>
        /// Returns a <see cref="Recipes.Maybe{T}" /> that does not contain a value.
        /// </summary>
        public static Maybe<T> No()
        {
            return no;
        }

        /// <summary>
        /// Gets the value contained by the <see cref="Recipes.Maybe{T}" />.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException("The Maybe does not contain a value.");
                }
                return value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has a value.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has value; otherwise, <c>false</c>.
        /// </value>
        public bool HasValue { get; private set; }
    }

    /// <summary>
    /// A delegate used to return an out parameter from a Try* method that indicates success via a boolean return value.
    /// </summary>
    /// <typeparam name="T">The type of the source object.</typeparam>
    /// <typeparam name="TOut">The type of the out parameter.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="outValue">The out parameter's value.</param>
    /// <returns>true if the out parameter was set; otherwise, false.</returns>
    internal delegate bool TryGetOutParameter<in T, TOut>(T source, out TOut outValue);

    /// <summary>
    /// A type representing a void return type.
    /// </summary>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal struct Unit
    {
        /// <summary>
        /// The default instance.
        /// </summary>
        public static readonly Unit Default = new Unit();
    }
}
