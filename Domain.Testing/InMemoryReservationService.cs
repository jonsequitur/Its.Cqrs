// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Sql;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// An in-memory reservation service 
    /// </summary>
    public class InMemoryReservationService : IReservationService, IQueryReservationService
    {
        private readonly ConcurrentDictionary<Tuple<string, string>, ReservedValue> reservedValues = new ConcurrentDictionary<Tuple<string, string>, ReservedValue>();

        public async Task<bool> Reserve(string value, string scope, string ownerToken, TimeSpan? lease = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
            if (ownerToken == null)
            {
                throw new ArgumentNullException("ownerToken");
            }

            var now = Clock.Now();
            var expiration = now + (lease ?? TimeSpan.FromMinutes(1));
            var key = Tuple.Create(value, scope);
            ReservedValue reservedValueInDictionary;
            reservedValues.TryGetValue(key, out reservedValueInDictionary);

            // Make sure to create a new object. Don't use the object from Dictionary directly.
            var reservedValue = reservedValueInDictionary == null ? null : new ReservedValue(reservedValueInDictionary);

            if (reservedValue == null)
            {
                // if not, create a new ticket
                reservedValue = new ReservedValue
                {
                    OwnerToken = ownerToken,
                    Scope = scope,
                    Value = value,
                    Expiration = expiration,
                    ConfirmationToken = value
                };
                return reservedValues.TryAdd(key, reservedValue);
            }
            else if (reservedValue.Expiration == null)
            {
                return reservedValue.OwnerToken == ownerToken;
            }
            else if (reservedValue.OwnerToken == ownerToken)
            {
                // if it's the same, extend the lease
                var newReservedValue = new ReservedValue(reservedValue) {Expiration = expiration};
                return reservedValues.TryUpdate(key, newReservedValue, reservedValue);
            }
            else if (reservedValue.Expiration < now)
            {
                // take ownership if the reserved value has expired
                var newReservedValue = new ReservedValue(reservedValue) { OwnerToken = ownerToken, Expiration = expiration };
                return reservedValues.TryUpdate(key, newReservedValue, reservedValue);
            }

            return false;
        }

        public async Task<bool> Confirm(string value, string scope, string ownerToken)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
            if (ownerToken == null)
            {
                throw new ArgumentNullException("ownerToken");
            }

            var reservedValueInDictionary = reservedValues.SingleOrDefault(kvp => 
                kvp.Value.Scope == scope &&
                kvp.Value.ConfirmationToken == value && 
                kvp.Value.OwnerToken == ownerToken).Value;

            if (reservedValueInDictionary != null)
            {
                // Make sure to create a new object. Don't use the object from Dictionary directly.
                var oldReservedValue = new ReservedValue(reservedValueInDictionary);    
                var newReservedValue = new ReservedValue(oldReservedValue) { Expiration = null };
                var key = Tuple.Create(newReservedValue.Value, newReservedValue.Scope);
                return reservedValues.TryUpdate(key, newReservedValue, oldReservedValue);
            }

            return false;
        }

        public async Task<bool> Cancel(string value, string scope, string ownerToken)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
            if (ownerToken == null)
            {
                throw new ArgumentNullException("ownerToken");
            }

            var key = Tuple.Create(value, scope);
            ReservedValue reservedValueInDictionary;

            if (reservedValues.TryGetValue(key, out reservedValueInDictionary))
            {
                // Make sure to create a new object. Don't use the object from Dictionary directly.
                var oldReservedValue = new ReservedValue(reservedValueInDictionary);
                if (oldReservedValue.OwnerToken == ownerToken)
                {
                    return reservedValues.TryRemove(key, out oldReservedValue);
                }
            }

            return false;
        }

        public async Task<string> ReserveAny(string scope, string ownerToken, TimeSpan? lease = null, string confirmationToken = null)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
            if (ownerToken == null)
            {
                throw new ArgumentNullException("ownerToken");
            }

            var now = Clock.Now();
            var expiration = now + (lease ?? TimeSpan.FromMinutes(1));
            ReservedValue newReservedValue;
            do
            {
                var reservedValueInDictionary = reservedValues.SingleOrDefault(kvp => kvp.Value.OwnerToken == ownerToken &&
                                                                 kvp.Value.ConfirmationToken == confirmationToken &&
                                                                 kvp.Value.Expiration != null).Value;
                if (reservedValueInDictionary == null)
                {
                    reservedValueInDictionary = reservedValues.FirstOrDefault(kvp => kvp.Value.Scope == scope &&
                                                                          kvp.Value.Expiration < now &&
                                                                          kvp.Value.Expiration != null).Value;
                }
                if (reservedValueInDictionary == null)
                {
                    return null;
                }

                // Make sure to create a new object. Don't use the object from Dictionary directly.
                var oldReservedValue = new ReservedValue(reservedValueInDictionary);
                newReservedValue = new ReservedValue(oldReservedValue)
                {
                    Expiration = expiration,
                    OwnerToken = ownerToken
                };

                if (confirmationToken != null)
                {
                    newReservedValue.ConfirmationToken = confirmationToken;
                }

                var key = Tuple.Create(newReservedValue.Value, newReservedValue.Scope);
                if (reservedValues.TryUpdate(key, newReservedValue, oldReservedValue))
                {
                    if (HasUniquenessConstraintViolation(newReservedValue.Scope, newReservedValue.ConfirmationToken))
                    {
                        // put the old Value back when there is a uniqueness violation
                        reservedValues.TryUpdate(key, oldReservedValue, newReservedValue);
                        return null;
                    }
                    return newReservedValue.Value;
                }
            } while (newReservedValue != null);
            
            return null;
        }

        // Scope + ConfirmationToken have to be unique in the dictionary
        private bool HasUniquenessConstraintViolation(string scope, string confirmationToken)
        {
            var reservations = reservedValues.Where(kvp =>
                kvp.Value.Scope == scope &&
                kvp.Value.ConfirmationToken == confirmationToken);

            return reservations.Count() > 1;
        }

        public async Task<ReservedValue> GetReservedValue(string value, string scope)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }

            var key = Tuple.Create(value, scope);
            ReservedValue reservedValue;
            reservedValues.TryGetValue(key, out reservedValue);
            return reservedValue;
        }
    }
}