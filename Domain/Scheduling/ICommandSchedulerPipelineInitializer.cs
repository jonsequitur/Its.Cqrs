// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Configures the command scheduler pipeline.
    /// </summary>
    internal interface ICommandSchedulerPipelineInitializer
    {
        /// <summary>
        /// Initializes the command scheduler in the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void Initialize(Configuration configuration);
    }
}