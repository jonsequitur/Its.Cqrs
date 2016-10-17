// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides access to system time.
    /// </summary>
    [DebuggerStepThrough]
    public class SystemClock : IClock
    {
        /// <summary>
        /// A single instance of the system clock.
        /// </summary>
        public static readonly SystemClock Instance = new SystemClock();

        private SystemClock()
        {
        }

        /// <summary>
        /// Gets the current time via <see cref="DateTimeOffset.Now" />.
        /// </summary>
        public DateTimeOffset Now() => DateTimeOffset.UtcNow;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{GetType()}: {Now():O}";
    }
}
