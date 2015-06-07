// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Sql;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    public class SqlQueryReservationService : IQueryReservationService
    {
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

            using (var db = new ReservationServiceDbContext())
            {
                return await db.Set<ReservedValue>()
                    .SingleOrDefaultAsync(v => v.Scope == scope && v.Value == value);
            }
        }
    }
}