// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Defines a mapping to a clock based on some value.
    /// </summary>
    public class ClockMapping
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the clock to which the mapping refers.
        /// </summary>
        public Clock Clock { get; set; }

        /// <summary>
        /// Gets or sets a value to be associated with the clock.
        /// </summary>
        public string Value { get; set; }
    }
}
