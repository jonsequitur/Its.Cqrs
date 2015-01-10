// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides access to time for the domain.
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Gets the current time.
        /// </summary>
        DateTimeOffset Now();
    }
}
