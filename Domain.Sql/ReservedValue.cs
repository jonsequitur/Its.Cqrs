// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain.Sql
{
    public class ReservedValue
    {
        private static readonly Lazy<Func<ReservedValue, ReservedValue>> cloneReservedValue =
            new Lazy<Func<ReservedValue, ReservedValue>>(
                () => MappingExpression.From<ReservedValue>
                    .ToNew<ReservedValue>()
                    .Compile());

        public string OwnerToken { get; set; }

        public string Value { get; set; }

        [Index("IX_ReservedValues_ConfirmationToken_Scope", 1, IsUnique = true)]
        public string ConfirmationToken { get; set; }

        [Index("IX_ReservedValues_ConfirmationToken_Scope", 2, IsUnique = true)]
        public string Scope { get; set; }

        public DateTimeOffset? Expiration { get; set; }

        public ReservedValue Clone() => 
            cloneReservedValue.Value(this);

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

        protected bool Equals(ReservedValue other) =>
            string.Equals(OwnerToken, other.OwnerToken) &&
            string.Equals(Value, other.Value) &&
            string.Equals(ConfirmationToken, other.ConfirmationToken) &&
            string.Equals(Scope, other.Scope) &&
            IsEqualTo(Expiration, other.Expiration);

        private static bool IsEqualTo<T>(Nullable<T> first, Nullable<T> second) where T : struct, IEquatable<T>
        {
            // if one is null, the other is not, then it's not equal
            if (first.HasValue != second.HasValue)
            {
                return false;
            }

            // Both either null or not null, then they're equal
            if (first.HasValue == false)
            {
                return true;}

            return first.Value.Equals(second);
        }

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
