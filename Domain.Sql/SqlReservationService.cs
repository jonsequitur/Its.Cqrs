using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// A reservation service backed by a SQL store.
    /// </summary>
    public class SqlReservationService : IReservationService
    {
        public Task<bool> Reserve(string value, string scope, string ownerToken, TimeSpan? lease = null)
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

            return Task.Run(() =>
            {
                using (var db = CreateReservationServiceDbContext())
                {
                    var reservedValues = db.Set<ReservedValue>();

                    var expiration = now + (lease.HasValue ? lease.Value : TimeSpan.FromMinutes(1));

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
                        db.SaveChanges();

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
            });
        }

        public Task<bool> Confirm(string value, string scope, string ownerToken)
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

            return Task.Run(() =>
            {
                using (var db = CreateReservationServiceDbContext())
                {
                    var reservedValue = db.Set<ReservedValue>()
                                          .SingleOrDefault(v => v.Scope == scope && v.ConfirmationToken == value);

                    if (reservedValue != null)
                    {
                        if (VerifyOwnerToken(ownerToken, reservedValue))
                        {
                            reservedValue.Expiration = null;
                            db.SaveChanges();
                            return true;
                        }

                        return false;
                    }

                    return true;
                }
            });
        }

        public Task<bool> Cancel(string value, string scope, string ownerToken)
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

            return Task.Run(() =>
            {
                using (var db = CreateReservationServiceDbContext())
                {
                    var reservedValues = db.Set<ReservedValue>();

                    var reservedValue = reservedValues
                        .SingleOrDefault(v => v.Scope == scope && v.Value == value);

                    if (reservedValue != null)
                    {
                        if (VerifyOwnerToken(ownerToken, reservedValue))
                        {
                            reservedValues.Remove(reservedValue);
                            db.SaveChanges();
                            return true;
                        }
                    }

                    return false;
                }
            });
        }

        public Func<DbContext> CreateReservationServiceDbContext = () => new ReservationServiceDbContext();

        private static bool VerifyOwnerToken(string ownerToken, ReservedValue reservedValue)
        {
            return reservedValue.OwnerToken == ownerToken;
        }

        public delegate ReservedValue ReservationLookup(
            DbSet<ReservedValue> db,
            string scope,
            DateTimeOffset expiration); 

        public Func<DbSet<ReservedValue>, string, DateTimeOffset, ReservedValue> GetValueToReserve =
            (reservedValues, scope, now) =>
            reservedValues.FirstOrDefault(r => r.Scope == scope
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
                var expiration = now + (lease.HasValue ? lease.Value : TimeSpan.FromMinutes(1));

                ReservedValue valueToReserve = null;
                do
                {
                    valueToReserve = reservedValues.SingleOrDefault(r => r.OwnerToken == ownerToken && 
                                                                         r.ConfirmationToken == confirmationToken &&
                                                                         r.Expiration != null);

                    if (valueToReserve == null)
                    {
                        valueToReserve = GetValueToReserve(reservedValues, scope, now);
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
                        db.SaveChanges();
                        return valueToReserve.Value;
                    }
                    catch (Exception exception)
                    {
                        if (!exception.IsConcurrencyException() || exception.IsUniquenessConstraint())
                        {
                            throw;
                        }
                        db.Entry(valueToReserve).State = EntityState.Unchanged;
                    }
                } while (valueToReserve != null); //retry on concurrency exception
            }
            return null;
        }
    }
}
