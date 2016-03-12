// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Reserves values in order to help enforce unique values within the system.
    /// </summary>
    public interface ISynchronousReservationService
    {
        /// <summary>
        /// Attempts to reserve the specified value.
        /// </summary>
        /// <param name="value">The value to be reserved.</param>
        /// <param name="scope">The scope in which the value must be unique.</param>
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <param name="lease">The lease duration, after which the reservation expires.</param>
        /// <returns>A task whose result is true if the value has been reserved.</returns>
        bool Reserve(string value, string scope, string ownerToken, TimeSpan? lease = null);

        /// <summary>
        /// Confirms the reservation of a specified value.
        /// </summary>
        /// <param name="value">The value to be reserved.</param>  
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <param name="scope">The scope in which the value must be unique.</param>
        bool Confirm(string value, string scope, string ownerToken);

        /// <summary>
        /// Cancels the specified reservation of a specified value.
        /// </summary>
        /// <param name="value">The reserved value.</param>
        /// <param name="scope">The scope in which the reserved value must be unique.</param>
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <returns></returns>
        bool Cancel(string value, string scope, string ownerToken);

        /// <summary>
        /// Attempts to reserve the first available value within a certain scope
        /// </summary>
        /// <param name="scope">The scope in which a set of unique values have been registered</param>
        /// <param name="ownerToken">A token indicating the owner of the reservation, which must be provided in order to confirm or cancel the reservation.</param>
        /// <param name="lease">The lease duration, after which the reservation expires.</param>
        /// <param name="confirmationToken">user specified value that can be used for confirmation of the reservation</param>
        /// <returns></returns>
        string ReserveAny(string scope, string ownerToken, TimeSpan? lease = null, string confirmationToken = null);
    }
}