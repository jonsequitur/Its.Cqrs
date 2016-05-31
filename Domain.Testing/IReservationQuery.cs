// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Query from Reservation Service
    /// </summary>
    public interface IReservationQuery
    {
        /// <summary>
        /// Retrieve single reserved value from Reservation Service
        /// </summary>
        /// <param name="value">The reserved value.</param>
        /// <param name="scope">The scope in which the reserved value must be unique.</param>
        /// <returns>The ReservedValue object</returns>
        Task<ReservedValue> GetReservedValue(string value, string scope);
    }
}