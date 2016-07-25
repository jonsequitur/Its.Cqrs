// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NUnit.Framework.Interfaces;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    public class DisableCommandAuthorizationAttribute : DomainConfigurationAttribute
    {
        protected override void BeforeTest(ITest test, Configuration configuration)
        {
            Command<CustomerAccount>.AuthorizeDefault = (order, command) => true;
            Command<Order>.AuthorizeDefault = (order, command) => true;
            Command<NonEventSourcedCommandTarget>.AuthorizeDefault = (order, command) => true;
        }
    }
}