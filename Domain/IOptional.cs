// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     Represents a value that can be left unspecified while other properties are being set, including to null.
    /// </summary>
    public interface IOptional
    {
        /// <summary>
        ///     Gets a value indicating whether a value has been set.
        /// </summary>
        bool IsSet { get; }

        /// <summary>
        ///     Gets the value.
        /// </summary>
        object Value { get; }
    }
}