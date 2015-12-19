// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    [Obsolete("Support will be removed in a forthcoming version. Please use the command scheduler instead.")]
    public static class Consequenter
    {
        public static IHaveConsequencesWhen<TEvent> Create<TEvent>(Action<TEvent> onEvent) where TEvent : IEvent
        {
            return new AnonymousConsequenter<TEvent>(onEvent);
        }
    }
}
