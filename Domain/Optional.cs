// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Methods for working with optionals.
    /// </summary>
    public static class Optional
    {
        /// <summary>
        ///  Creates an optional containing the specified value.
        /// </summary>
        public static Optional<T> Create<T>(T value) => new Optional<T>(value);
    }
}
