// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    ///     A reservation service backed by a SQL store.
    /// </summary>
    public class SqlReservationService : IReservationService
    {
        private readonly Func<ReservationServiceDbContext> createReservationServiceDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlReservationService"/> class.
        /// </summary>
        /// <param name="createReservationServiceDbContext">A delegate used to create reservation service database context instances.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public SqlReservationService(Func<ReservationServiceDbContext> createReservationServiceDbContext)
        {
            if (createReservationServiceDbContext == null)
            {
                throw new ArgumentNullException(nameof(createReservationServiceDbContext));
            }
            this.createReservationServiceDbContext = createReservationServiceDbContext;
        }

        /// <summary>
        /// Attempts to reserve the specified value.
        /// </summary>
        /// <param name="value">The value to be reserved.</param>
        /// <param name="scope">The scope in which the value must be unique.</param>
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <param name="lease">The lease duration, after which the reservation expires.</param>
        /// <returns>A task whose result is true if the value has been reserved.</returns>
        public async Task<bool> Reserve(string value, string scope, string ownerToken, TimeSpan? lease = null)
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

            using (var db = createReservationServiceDbContext())
            {
                var reservedValues = db.Set<ReservedValue>();

                var expiration = now + (lease ?? TimeSpan.FromMinutes(1));

                // see if there is a pre-existing lease by the same actor
                var reservedValue = await reservedValues.SingleOrDefaultAsync(r => r.Scope == scope &&
                                                                                   r.Value == value);

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
                    reservedValues.Add(reservedValue);
                }
                else if (reservedValue.Expiration == null)
                {
                    return reservedValue.OwnerToken == ownerToken;
                }
                else if (reservedValue.OwnerToken == ownerToken)
                {
                    // if it's the same, extend the lease
                    reservedValue.Expiration = expiration;
                }
                else if (reservedValue.Expiration < now)
                {
                    // take ownership if the reserved value has expired
                    reservedValue.OwnerToken = ownerToken;
                    reservedValue.Expiration = expiration;
                }
                else
                {
                    return false;
                }

                try
                {
                    await db.SaveChangesAsync();

                    return true;
                }
                catch (Exception exception)
                {
                    if (!exception.IsConcurrencyException())
                    {
                        throw;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Confirms the reservation of a specified value.
        /// </summary>
        /// <param name="value">The value to be reserved.</param>  
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <param name="scope">The scope in which the value must be unique.</param>
        public async Task<bool> Confirm(string value, string scope, string ownerToken)
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

            using (var db = createReservationServiceDbContext())
            {
                var reservedValue = await db.ReservedValues
                                            .SingleOrDefaultAsync(v => v.Scope == scope &&
                                                                       v.ConfirmationToken == value &&
                                                                       v.OwnerToken == ownerToken);

                if (reservedValue != null)
                {
                    reservedValue.Expiration = null;
                    await db.SaveChangesAsync();
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Cancels the specified reservation of a specified value.
        /// </summary>
        /// <param name="value">The reserved value.</param>
        /// <param name="scope">The scope in which the reserved value must be unique.</param>
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <returns></returns>
        public async Task<bool> Cancel(string value, string scope, string ownerToken)
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

            using (var db = createReservationServiceDbContext())
            {
                var reservedValues = db.ReservedValues;

                var reservedValue = await reservedValues
                                              .SingleOrDefaultAsync(v => v.Scope == scope && v.Value == value && v.OwnerToken == ownerToken);

                if (reservedValue != null)
                {
                    reservedValues.Remove(reservedValue);
                    await db.SaveChangesAsync();
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Attempts to reserve the first available value within a certain scope
        /// </summary>
        /// <param name="scope">The scope in which a set of unique values have been registered</param>
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <param name="lease">The lease duration, after which the reservation expires.</param>
        /// <param name="confirmationToken">user specified value that can be used for confirmation of the reservation</param>
        /// <returns></returns>
        public async Task<string> ReserveAny(string scope, string ownerToken, TimeSpan? lease = null, string confirmationToken = null)
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

            using (var db = createReservationServiceDbContext())
            {
                var reservedValues = db.ReservedValues;
                var expiration = now + (lease ?? TimeSpan.FromMinutes(1));

                ReservedValue valueToReserve;
                do
                {
                    valueToReserve = await reservedValues.SingleOrDefaultAsync(r => r.OwnerToken == ownerToken &&
                                                                                    r.ConfirmationToken == confirmationToken &&
                                                                                    r.Expiration != null);

                    if (valueToReserve == null)
                    {
                        valueToReserve =
                            await reservedValues.FirstOrDefaultAsync(
                                r => r.Scope == scope
                                     && r.Expiration < now
                                     && r.Expiration != null);
                    }

                    if (valueToReserve == null)
                    {
                        return null;
                    }

                    valueToReserve.Expiration = expiration;
                    valueToReserve.OwnerToken = ownerToken;

                    if (confirmationToken != null)
                    {
                        valueToReserve.ConfirmationToken = confirmationToken;
                    }

                    try
                    {
                        await db.SaveChangesAsync();
                        return valueToReserve.Value;
                    }
                    catch (DbUpdateException exception)
                    {
                        if (exception.InnerException is OptimisticConcurrencyException)
                        {
                            db.Entry(valueToReserve).State = EntityState.Unchanged;
                        }
                        else if (exception.IsUniquenessConstraint())
                        {
                            return null;
                        }
                        else
                        {
                            throw;
                        }
                    }
                } while (valueToReserve != null); //retry on concurrency exception
            }
            return null;
        }

        /// <summary>
        /// Retrieve single reserved value from Reservation Service
        /// </summary>
        /// <param name="value">The reserved value.</param>
        /// <param name="scope">The scope in which the reserved value must be unique.</param>
        /// <returns>The ReservedValue object</returns>
        public async Task<ReservedValue> GetReservedValue(string value, string scope)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            using (var db = createReservationServiceDbContext())
            {
                return await db.ReservedValues
                               .SingleOrDefaultAsync(v => v.Scope == scope && v.Value == value);
            }
        }
    }
}