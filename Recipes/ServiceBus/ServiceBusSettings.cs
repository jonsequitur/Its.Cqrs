// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Its.Domain.ServiceBus
{
    /// <summary>
    /// Provides settings for accessing the Azure Service Bus.
    /// </summary>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    public class ServiceBusSettings
    {
        /// <summary>
        /// Gets or sets the Azure Service Bus connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the maximum concurrent sessions for message receivers.
        /// </summary>
        public int? MaxConcurrentSessions { get; set; }

        /// <summary>
        /// Gets or sets the name prefix for topics and queues created using these settings.
        /// </summary>
        public string NamePrefix { get; set; }

        /// <summary>
        /// Provides a way for queue configuration to be specified programmatically at queue creation time.
        /// </summary>
        public Action<QueueDescription> ConfigureQueue = q => { };
    }
}
