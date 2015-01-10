// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides a way to differentiate event handlers having the same type but different implementations, e.g. anonymous and composite handlers.
    /// </summary>
    public interface INamedEventHandler 
    {
        string Name { get; }
    }
}
