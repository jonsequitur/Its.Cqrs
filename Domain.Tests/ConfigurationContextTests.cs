// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class ConfigurationContextTests
    {
        [Test]
        public void Configuration_Current_returns_the_Configuration_set_in_ConfigurationContext_Establish()
        {
            var configuration = new Configuration();
            using (var context = ConfigurationContext.Establish(configuration))
            {
                Configuration.Current.Should().BeSameAs(configuration);
                Configuration.Current.Should().BeSameAs(context.Configuration);
            }
        }

        [Test]
        public void When_ConfigurationContext_Establish_has_not_been_called_then_Configuration_Current_returns_the_global_Configuration()
        {
            Configuration.Current.Should().BeSameAs(Configuration.Global);
        }

        [Test]
        public void Configuration_Contexts_cannot_be_nested()
        {
            using (ConfigurationContext.Establish(new Configuration()))
            {
                Action establishAnother = () => ConfigurationContext.Establish(new Configuration());

                establishAnother.ShouldThrow<InvalidOperationException>();
            }
        }
    }
}
