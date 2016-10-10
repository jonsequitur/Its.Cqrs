// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Indicates that the implementing object supports extensible metadata.
    /// </summary>
    public interface IHaveExtensibleMetada
    {
        /// <summary>
        /// Gets a dynamic metadata property bag.
        /// </summary>
        dynamic Metadata { get; }
    }
}
