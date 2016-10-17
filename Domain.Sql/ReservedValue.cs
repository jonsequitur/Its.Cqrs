// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// A value that has been reserved or for which a reservation attempt has been made.
    /// </summary>
    public class ReservedValue
    {
        private static readonly Lazy<Func<ReservedValue, ReservedValue>> cloneReservedValue =
            new Lazy<Func<ReservedValue, ReservedValue>>(
                () => MappingExpression.From<ReservedValue>
                                       .ToNew<ReservedValue>()
                                       .Compile());

        /// <summary>
        /// A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.
        /// </summary>
        public string OwnerToken { get; set; }

        /// <summary>
        /// Gets or sets the value to be reserved.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets a token which must be used when confirming the reservation.
        /// </summary>
        [Index("IX_ReservedValues_ConfirmationToken_Scope", 1, IsUnique = true)]
        public string ConfirmationToken { get; set; }

        /// <summary>
        /// Gets or sets the scope in which the value must be unique.
        /// </summary>
        [Index("IX_ReservedValues_ConfirmationToken_Scope", 2, IsUnique = true)]
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the time at which the reservation attempt expires and the value becomes available for someone else to reserve.
        /// </summary>
        public DateTimeOffset? Expiration { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        public ReservedValue Clone() =>
            cloneReservedValue.Value(this);

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <filterpriority>2</filterpriority>
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
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((ReservedValue) obj);
        }

        /// <summary>Determines whether the specified reserved value is equal to the current reserved value.</summary>
        protected bool Equals(ReservedValue other) =>
            string.Equals(OwnerToken, other.OwnerToken) &&
            string.Equals(Value, other.Value) &&
            string.Equals(ConfirmationToken, other.ConfirmationToken) &&
            string.Equals(Scope, other.Scope) &&
            IsEqualTo(Expiration, other.Expiration);

        private static bool IsEqualTo<T>(T? first, T? second) where T : struct, IEquatable<T>
        {
            // if one is null, the other is not, then it's not equal
            if (first.HasValue != second.HasValue)
            {
                return false;
            }

            // Both either null or not null, then they're equal
            if (first.HasValue == false)
            {
                return true;
            }

            return first.Value.Equals(second);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode*397) ^ (OwnerToken?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (Value?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (ConfirmationToken?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (Scope?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (Expiration?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}