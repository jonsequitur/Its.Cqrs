// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Sql;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    public class SqlReservationQuery : IReservationQuery
    {
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

            using (var db = new ReservationServiceDbContext())
            {
                return await db.Set<ReservedValue>()
                    .SingleOrDefaultAsync(v => v.Scope == scope && v.Value == value);
            }
        }
    }
}