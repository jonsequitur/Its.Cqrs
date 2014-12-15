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
        public bool IsSet
        {
            get
            {
                return isSet;
            }
        }

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }

        public bool Equals(Optional<T> other)
        {
            return EqualityComparer<T>.Default.Equals(value, other.value) && isSet.Equals(other.isSet);
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>
        /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name="obj">Another object to compare to. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is Optional<T> && Equals((Optional<T>) obj);
        }

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

        public static bool operator ==(Optional<T> left, object right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Optional<T> left, object right)
        {
            return !(left == right);
        }

        /// <summary>
        ///     Gets the value.
        /// </summary>
        object IOptional.Value
        {
            get
            {
                return Value;
            }
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return IsSet
                       ? (Value != null ? Value.ToString() : string.Format("Optional<{0}> (set to null)", typeof(T).Name))
                       : string.Format("Optional<{0}> (not set)", typeof(T).Name);
        }

        /// <summary>
        ///     Represents the unset value for this type.
        /// </summary>
        public static readonly Optional<T> Unset = new Optional<T>();

        /// <summary>
        /// Creates an <see cref="Optional{T}" /> containing the specified value.
        /// </summary>
        /// <param name="value">The optional value.</param>
        public static Optional<T> Create(T value)
        {
            return new Optional<T>(value);
        }
    }
}