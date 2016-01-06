// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Its.Domain.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     Defines a class that can be used like a string but with additional constraints to enforce specific semantic usages.
    /// </summary>
    /// <typeparam name="T">The type of the implementing class.</typeparam>
    [DebuggerDisplay("{Value}")]
    [Serializable]
    [JsonConverter(typeof(PrimitiveConverter))]
    public abstract class String<T> where T : String<T>
    {
        private readonly string value;
        private readonly StringComparison stringComparison = StringComparison.OrdinalIgnoreCase;
        private static readonly Func<string, T> create;

        /// <summary>
        ///     Initializes the <see cref="String{T}" /> class.
        /// </summary>
        static String()
        {
            var ctor = typeof (T).GetConstructor(new[] { typeof (string) });
            var innerValue = Expression.Parameter(typeof (string));
            var expression = Expression.Lambda<Func<string, T>>(
                Expression.New(ctor, innerValue), innerValue);
            create = expression.Compile();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="String&lt;T&gt;" /> class.
        /// </summary>
        protected String()
        {
            value = string.Empty;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="String&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="value">The value held by the string instance.</param>
        /// <param name="stringComparison">
        ///     The string comparison to be used to determine equality of two instances of the same <see cref="String{T}" />.
        /// </param>
        protected String(string value, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            this.value = value;
            this.stringComparison = stringComparison;
        }

        /// <summary>
        ///     Gets the underlying <see cref="string" /> value of the instance.
        /// </summary>
        public string Value
        {
            get
            {
                return value;
            }
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="String{T}" /> to <see cref="System.String" />.
        /// </summary>
        /// <param name="from">From.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator string(String<T> from)
        {
            return from.Value;
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.String" /> to <see cref="String{T}" />.
        /// </summary>
        /// <param name="from">From.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator String<T>(string from)
        {
            return create(from);
        }

        /// <summary>
        ///     Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">
        ///     The <see cref="System.Object" /> to compare with this instance.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(this, obj);
        }

        /// <summary>
        ///     Equalses the specified other.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public bool Equals(String<T> other)
        {
            return Equals(this, other);
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return Value.ToLowerInvariant().GetHashCode() ^ typeof (T).GetHashCode();
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        ///     Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator ==(String<T> left, object right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///     Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator !=(String<T> left, object right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        ///     Equalses the specified left.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static bool Equals(String<T> left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(null, right))
            {
                return false;
            }

            if (ReferenceEquals(null, left))
            {
                return false;
            }

            if (left.GetType() != right.GetType())
            {
                var s = right as string;
                if (s != null)
                {
                    // if right is a true string, compare as string
                    return string.Equals(
                        left.Value,
                        s,
                        left.stringComparison);
                }

                // two different semantic string types are never equal
                return false;
            }

            // if they're the same type, compare as string
            return string.Equals(
                left.Value,
                ((String<T>) right).Value,
                left.stringComparison);
        }
    }
}
