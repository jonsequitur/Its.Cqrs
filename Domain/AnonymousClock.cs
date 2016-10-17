// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    internal class AnonymousClock : IClock
    {
        private readonly Func<DateTimeOffset> now;

        public AnonymousClock(
            Func<DateTimeOffset> now,
            IClock parentClock = null)
        {
            if (now == null)
            {
                throw new ArgumentNullException(nameof(now));
            }
            this.now = now;
            ParentClock = parentClock;
        }

        public string Name { get; set; }

        public DateTimeOffset Now() => now();

        internal IClock ParentClock { get; }

        public override string ToString()
        {
            return string.Format("{0}: {2}{1}",
                GetType(),
                Name.IfNotNullOrEmptyOrWhitespace()
                    .Then(n => $" ({n})")
                    .ElseDefault(),
                Now().ToString("O"));
        }
    }
}