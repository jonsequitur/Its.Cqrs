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
    /// A reservation service backed by a SQL store.
    /// </summary>
    public class SqlReservationService : IReservationService
    {
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

            using (var db = CreateReservationServiceDbContext())
            {
                var reservedValues = db.Set<ReservedValue>();

                var expiration = now + (lease ?? TimeSpan.FromMinutes(1));

                // see if there is a pre-existing lease by the same actor
                var reservedValue = reservedValues.SingleOrDefault(r => r.Scope == scope &&
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

            using (var db = CreateReservationServiceDbContext())
            {
                var reservedValue = await db.Set<ReservedValue>()
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

            using (var db = CreateReservationServiceDbContext())
            {
                var reservedValues = db.Set<ReservedValue>();

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

        public Func<DbContext> CreateReservationServiceDbContext = () => new ReservationServiceDbContext();

        public Func<DbSet<ReservedValue>, string, DateTimeOffset, Task<ReservedValue>> GetValueToReserve =
            async (reservedValues, scope, now) =>
                await reservedValues.FirstOrDefaultAsync(r => r.Scope == scope
                                                              && r.Expiration < now
                                                              && r.Expiration != null);

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

            using (var db = CreateReservationServiceDbContext())
            {
                var reservedValues = db.Set<ReservedValue>();
                var expiration = now + (lease ?? TimeSpan.FromMinutes(1));

                ReservedValue valueToReserve;
                do
                {
                    valueToReserve = await reservedValues.SingleOrDefaultAsync(r => r.OwnerToken == ownerToken &&
                                                                                    r.ConfirmationToken == confirmationToken &&
                                                                                    r.Expiration != null);

                    if (valueToReserve == null)
                    {
                        valueToReserve = await GetValueToReserve(reservedValues, scope, now);
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
    }
}