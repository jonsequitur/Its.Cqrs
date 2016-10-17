// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Represents a value that may or may not have been set.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <remarks>This class can be used to distinguish whether a value was modified, for example allowing a distinction to be made between null and "not modified". This is useful for command and event definitions, allowing an existing value for a property to be unchanged if not specified. It also aids compatibility with JavaScript/JSON, which includes the notion of undefined which is not present in .NET.</remarks>
    public struct Optional<T> : IOptional
    {
        private T value;
        private bool isSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="Optional{T}"/> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Optional(T value)
        {
            this.value = value;
            isSet = true;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        /// <exception cref="System.InvalidOperationException">Optional has no value. Check whether IsSet returns true before calling Value.</exception>
        public T Value
        {
            get
            {
                if (!IsSet)
                {
                    throw new InvalidOperationException("Optional has no value. Check whether IsSet returns true before calling Value.");
                }
                return value;
            }
            set
            {
                this.value = value;
                isSet = true;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether a value has been set.
        /// </summary>
        public bool IsSet => isSet;

        /// <summary>
        /// Performs an implicit conversion from <see typeparamref="T"/> to <see cref="Optional{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Optional<T>(T value) => new Optional<T>(value);

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        public bool Equals(Optional<T> other) => EqualityComparer<T>.Default.Equals(value, other.value) && isSet.Equals(other.isSet);

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>
        /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name="obj">Another object to compare to. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj) =>
            !ReferenceEquals(null, obj) &&
            obj is Optional<T> &&
            Equals((Optional<T>) obj);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(value)*397) ^ isSet.GetHashCode();
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Optional<T> left, object right) => Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Optional<T> left, object right) => !(left == right);

        /// <summary>
        ///     Gets the value.
        /// </summary>
        object IOptional.Value => Value;

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return IsSet
                       ? (Value != null ? Value.ToString() : $"Optional<{typeof (T).Name}> (set to null)")
                       : $"Optional<{typeof (T).Name}> (not set)";
        }

        /// <summary>
        ///     Represents the unset value for this type.
        /// </summary>
        public static readonly Optional<T> Unset = new Optional<T>();

        /// <summary>
        /// Creates an <see cref="Optional{T}" /> containing the specified value.
        /// </summary>
        /// <param name="value">The optional value.</param>
        public static Optional<T> Create(T value) => new Optional<T>(value);
    }
}
