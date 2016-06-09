// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    internal class BackgroundWork
    {
        public BackgroundWork(Action<Configuration> start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            Start = start;
        }

        public BackgroundWork(Func<Configuration, IDisposable> start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            Start = configuration =>
                    configuration.RegisterForDisposal(start(configuration));
        }

        public Action<Configuration> Start { get; }
    }
}