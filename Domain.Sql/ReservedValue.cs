// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    public class ReservedValue
    {
        public ReservedValue()
        {
        }

        public ReservedValue(ReservedValue other)
        {
            OwnerToken = other.OwnerToken;
            Value = other.Value;
            ConfirmationToken = other.ConfirmationToken;
            Scope = other.Scope;
            Expiration = other.Expiration;
        }

        public string OwnerToken { get; set; }
        
        public string Value { get; set; }

        public string ConfirmationToken { get; set; }

        public string Scope { get; set; }

        public DateTimeOffset? Expiration { get; set; }

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
            return Equals((ReservedValue)obj);
        }

        protected bool Equals(ReservedValue other)
        {
            return string.Equals(OwnerToken, other.OwnerToken) &&
                   string.Equals(Value, other.Value) &&
                   string.Equals(ConfirmationToken, other.ConfirmationToken) &&
                   string.Equals(Scope, other.Scope) &&
                   IsEqualTo(Expiration, other.Expiration);
        }

        private static bool IsEqualTo<T>(Nullable<T> first, Nullable<T> second) where T : struct, IEquatable<T>
        {
            // if one is null, the other is not, then it's not equal
            if (first.HasValue != second.HasValue)
                return false;

            // Both either null or not null, then they're equal
            if (first.HasValue == false)
                return true;

            return first.Value.Equals(second);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode*397) ^ (OwnerToken != null ? OwnerToken.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ConfirmationToken != null ? ConfirmationToken.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Scope != null ? Scope.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Expiration.HasValue ? Expiration.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
