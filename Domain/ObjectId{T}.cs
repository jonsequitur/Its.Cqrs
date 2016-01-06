// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Its.Domain.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An object identifier applicable to a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the underlying value.</typeparam>
    /// <remarks>This class can help to prevent identifiers for one type from being used for a different type.</remarks>
    [DebuggerDisplay("{Value}")]
    [DebuggerStepThrough]
    [JsonConverter(typeof(PrimitiveConverter))]
    public abstract class ObjectId<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentException"></exception>
        protected ObjectId(T value)
        {
            if (Equals(value, default(T)))
            {
                throw new ArgumentException(GetType() + ".Value cannot be set to " + default(T));
            }
            Value = value;
        }

        /// <summary>
        /// Gets the value of the object id.
        /// </summary>
        public T Value { get; private set; }

        protected bool Equals(ObjectId<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is T)
            {
                return Equals(Value, (T) obj);
            }

            if (obj is ObjectId<T>)
            {
                return Equals((ObjectId<T>) obj);
            }

            return false;
        }

        /// <summary>
        ///     Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        public static bool operator ==(ObjectId<T> left, object right)
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
        public static bool operator !=(ObjectId<T> left, object right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
