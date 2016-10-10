// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// A persistable <see cref="IClock" />.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.IClock" />
    [DebuggerDisplay("{ToString()}")]
    public class Clock : IClock
    {
        /// <summary>
        /// Gets or sets the unique identifier of the clock.
        /// </summary>
        public int Id { get; set; }

          /// <summary>
        /// Gets or sets the unique name of the clock.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the time that the clock was set to when it was created.
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Gets or sets UTC time.
        /// </summary>
        public DateTimeOffset UtcNow { get; set; }

        /// <summary>
        /// Gets the current time.
        /// </summary>
        public DateTimeOffset Now() => UtcNow;

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => $"\"{Name}\": {UtcNow:O}";

        /// <summary>
        /// The name of the default clock.
        /// </summary>
        /// <remarks>Unless otherwise configured, scheduled commands will be scheduled against the clock having this name.</remarks>
        public const string DefaultClockName = "default";
    }
}
