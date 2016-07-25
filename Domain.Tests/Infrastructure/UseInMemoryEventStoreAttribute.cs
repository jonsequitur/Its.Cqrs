// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Testing;
using NUnit.Framework.Interfaces;

namespace Microsoft.Its.Domain.Tests
{
    public class UseInMemoryEventStoreAttribute : DomainConfigurationAttribute
    {
        protected override void BeforeTest(ITest test, Configuration configuration)
        {
            configuration.UseInMemoryEventStore();
        }
    }
}