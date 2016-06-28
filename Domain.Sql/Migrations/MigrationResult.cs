// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql.Migrations
{
    /// <summary>
    /// Returns the result of a database migrator.
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// Gets or sets the log output of the migration run, e.g. the contents of a SQL script or the results of a cleanup or seed operation.
        /// </summary>
        public string Log { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the migration was applied.
        /// </summary>
        /// <remarks>
        /// Some migrations may written to be periodic or depend on other migrations. These can be implemented by returning false, allowing a future migration to run to apply it.
        /// </remarks>
        public bool MigrationWasApplied { get; set; }
    }
}