// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides access to time for the domain.
    /// </summary>
    [DebuggerStepThrough]
    public static class Clock
    {
        private static IClock current = SystemClock.Instance;

        /// <summary>
        /// Initializes the <see cref="Clock"/> class.
        /// </summary>
        static Clock()
        {
            Reset();
        }

        /// <summary>
        /// Gets the current time.
        /// </summary>
        public static Func<DateTimeOffset> Now
        {
            get
            {
                return Current.Now;
            }
            set
            {
                if (value == null)
                {
                    Reset();
                }
                else
                {
                    current = Create(value);
                }
            }
        }

        public static void Reset()
        {
            Current = SystemClock.Instance;
        }

        /// <summary>
        /// Gets or sets the current clock.
        /// </summary>
        public static IClock Current
        {
            get
            {
                var commandContext = CommandContext.Current;
                if (commandContext != null)
                {
                    return commandContext.Clock;
                }
                return current;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                current = value;
            }
        }

        /// <summary>
        /// Creates an <see cref="IClock" /> instance that calls the provided delegate to return the current time.
        /// </summary>
        public static IClock Create(Func<DateTimeOffset> now)
        {
            return new AnonymousClock(now);
        }

        internal static IClock Latest(params IClock[] clocks)
        {
            clocks = clocks.Where(c => c != null).ToArray();

            if (clocks.Count() == 1)
            {
                return clocks.Single();
            }

            return Create(() => clocks
                                    .Select(c => c.Now())
                                    .OrderBy(time => time)
                                    .Last());
        }

        internal static IClock Earliest(params IClock[] clocks)
        {
            clocks = clocks.Where(c => c != null).ToArray();

            if (clocks.Count() == 1)
            {
                return clocks.Single();
            }

            return Create(() => clocks.Select(c => c.Now())
                                      .OrderByDescending(time => time)
                                      .Last());
        }
    }
}
