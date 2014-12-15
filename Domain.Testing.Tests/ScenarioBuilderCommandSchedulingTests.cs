// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [Ignore("Test not finished")]
    [TestFixture]
    public class ScenarioBuilderCommandSchedulingTests
    {
        [SetUp]
        public void SetUp()
        {
            Command<CustomerAccount>.AuthorizeDefault = (account, command) => true;
        }

        [Test]
        public void In_memory_command_scheduling_is_enabled_by_default()
        {
            using (VirtualClock.Start())
            {
                // TODO: (In_memory_command_scheduling_is_enabled_by_default) 
                var scenario = new ScenarioBuilder().Prepare();

                scenario.Save(new CustomerAccount()
                                  .Apply(new ChangeEmailAddress(Any.Email()))
                                  .Apply(new SendMarketingEmailOn(Clock.Now().AddDays(1))));

                VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(1.0000001));

                var account = scenario.GetLatest<CustomerAccount>();

                account.Events().Last().Should().BeOfType<SentMarketingEmail>();
            }
        }
    }
}