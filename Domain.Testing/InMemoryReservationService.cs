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
    public class InMemoryReservationService : IReservationService
    {
        private readonly ConcurrentDictionary<Tuple<string, string>, ReservedValue> reservedValues = new ConcurrentDictionary<Tuple<string, string>, ReservedValue>();

        /// <summary>
        /// Attempts to reserve the specified value.
        /// </summary>
        /// <param name="value">The value to be reserved.</param>
        /// <param name="scope">The scope in which the value must be unique.</param>
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <param name="lease">The lease duration, after which the reservation expires.</param>
        /// <returns>A task whose result is true if the value has been reserved.</returns>
        public Task<bool> Reserve(string value, string scope, string ownerToken, TimeSpan? lease = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }
            if (ownerToken == null)
            {
                throw new ArgumentNullException(nameof(ownerToken));
            }

            var now = Clock.Now();
            var expiration = now + (lease ?? TimeSpan.FromMinutes(1));
            var key = Tuple.Create(value, scope);
            ReservedValue reservedValueInDictionary;
            reservedValues.TryGetValue(key, out reservedValueInDictionary);

            // Make sure to create a new object. Don't use the object from Dictionary directly.
            var reservedValue = reservedValueInDictionary?.Clone();

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
                return reservedValues.TryAdd(key, reservedValue).CompletedTask();
            }

            if (reservedValue.Expiration == null)
            {
                return (reservedValue.OwnerToken == ownerToken).CompletedTask();
            }

            if (reservedValue.OwnerToken == ownerToken)
            {
                // if it's the same, extend the lease
                var newReservedValue = reservedValue.Clone();
                newReservedValue.Expiration = expiration;
                return reservedValues.TryUpdate(key, newReservedValue, reservedValue).CompletedTask();
            }

            if (reservedValue.Expiration < now)
            {
                // take ownership if the reserved value has expired
                var newReservedValue = reservedValue.Clone();
                newReservedValue.OwnerToken = ownerToken;
                newReservedValue.Expiration = expiration;
                return reservedValues.TryUpdate(key, newReservedValue, reservedValue).CompletedTask();
            }

            return false.CompletedTask();
        }

        /// <summary>
        /// Confirms the reservation of a specified value.
        /// </summary>
        /// <param name="value">The value to be reserved.</param>  
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <param name="scope">The scope in which the value must be unique.</param>
        public Task<bool> Confirm(string value, string scope, string ownerToken)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }
            if (ownerToken == null)
            {
                throw new ArgumentNullException(nameof(ownerToken));
            }

            var reservedValueInDictionary = reservedValues.SingleOrDefault(kvp => 
                kvp.Value.Scope == scope &&
                kvp.Value.ConfirmationToken == value && 
                kvp.Value.OwnerToken == ownerToken).Value;

            if (reservedValueInDictionary != null)
            {
                // Make sure to create a new object. Don't use the object from Dictionary directly.
                var oldReservedValue = reservedValueInDictionary.Clone();
                var newReservedValue = oldReservedValue.Clone();
                newReservedValue.Expiration = null;
                var key = Tuple.Create(newReservedValue.Value, newReservedValue.Scope);
                return reservedValues.TryUpdate(key, newReservedValue, oldReservedValue).CompletedTask();
            }

            return false.CompletedTask();
        }

        /// <summary>
        /// Cancels the specified reservation of a specified value.
        /// </summary>
        /// <param name="value">The reserved value.</param>
        /// <param name="scope">The scope in which the reserved value must be unique.</param>
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <returns></returns>
        public Task<bool> Cancel(string value, string scope, string ownerToken)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }
            if (ownerToken == null)
            {
                throw new ArgumentNullException(nameof(ownerToken));
            }

            var key = Tuple.Create(value, scope);
            ReservedValue reservedValueInDictionary;

            if (reservedValues.TryGetValue(key, out reservedValueInDictionary))
            {
                // Make sure to create a new object. Don't use the object from Dictionary directly.
                var oldReservedValue = reservedValueInDictionary.Clone();
                if (oldReservedValue.OwnerToken == ownerToken)
                {
                    return reservedValues.TryRemove(key, out oldReservedValue).CompletedTask();
                }
            }

            return false.CompletedTask();
        }

        /// <summary>
        /// Attempts to reserve the first available value within a certain scope
        /// </summary>
        /// <param name="scope">The scope in which a set of unique values have been registered</param>
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <param name="lease">The lease duration, after which the reservation expires.</param>
        /// <param name="confirmationToken">user specified value that can be used for confirmation of the reservation</param>
        /// <returns></returns>
        public Task<string> ReserveAny(string scope, string ownerToken, TimeSpan? lease = null, string confirmationToken = null)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }
            if (ownerToken == null)
            {
                throw new ArgumentNullException(nameof(ownerToken));
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
                    return Task.FromResult<string>(null);
                }

                // Make sure to create a new object. Don't use the object from Dictionary directly.
                var oldReservedValue = reservedValueInDictionary.Clone();
                newReservedValue = oldReservedValue.Clone();
                newReservedValue.Expiration = expiration;
                newReservedValue.OwnerToken = ownerToken;

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
                    return newReservedValue.Value.CompletedTask();
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

        /// <summary>
        /// Gets a reserved value by its value and scope.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public Task<ReservedValue> GetReservedValue(string value, string scope)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            var key = Tuple.Create(value, scope);
            ReservedValue reservedValue;
            reservedValues.TryGetValue(key, out reservedValue);

            return reservedValue.CompletedTask();
        }
    }
}