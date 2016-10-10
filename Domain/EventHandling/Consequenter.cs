// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Methods for creating anonymous consequenters.
    /// </summary>
    [Obsolete("Support will be removed in a forthcoming version. Please use the command scheduler instead.")]
    public static class Consequenter
    {
        /// <summary>
        /// Creates an anonymous consequenter.
        /// </summary>
        public static IHaveConsequencesWhen<TEvent> Create<TEvent>(Action<TEvent> onEvent) where TEvent : IEvent =>
            new AnonymousConsequenter<TEvent>(onEvent);
    }
}
