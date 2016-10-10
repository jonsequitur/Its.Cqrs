// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides storage for entities that can have commands applied to them by the command scheduler.
    /// </summary>
    public interface IStore<T> 
        where T : class
    {
        /// <summary>
        ///     Gets a command target by the id.
        /// </summary>
        /// <param name="id">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        Task<T> Get(string id);

        /// <summary>
        ///     Persists the state of the command target.
        /// </summary>
        Task Put(T aggregate);
    }
}