// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Its.Domain
{
    internal class CompositeEventHandler : IEventHandler,
                                           INamedEventHandler
    {
        private readonly object[] projectors;

        public CompositeEventHandler(params object[] projectors)
        {
            if (projectors == null)
            {
                throw new ArgumentNullException(nameof(projectors));
            }
            this.projectors = projectors;
        }

        public IEnumerable<IEventHandlerBinder> GetBinders() => projectors.SelectMany(EventHandler.GetBinders);

        public string Name { get;  set; }
    }
}
