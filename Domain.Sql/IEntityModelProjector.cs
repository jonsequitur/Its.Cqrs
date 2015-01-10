// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Indicates a projector whose responsibility is to create db context instances.
    /// </summary>
    public interface IEntityModelProjector
    {
        /// <summary>
        /// Creates a db context.
        /// </summary>
        DbContext CreateDbContext();
    }
}
