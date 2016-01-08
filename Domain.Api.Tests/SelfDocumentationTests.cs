// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Microsoft.Its.Domain.Api.Documentation;
using Microsoft.Its.Domain.Api.Tests.Infrastructure;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Sample.Domain.Api.Controllers;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Api.Tests
{
    [TestFixture]
    public class SelfDocumentationTests
    {
        // this is a shim to make sure that the Sample.Domain.Api assembly is loaded into the AppDomain, otherwise Web API won't discover the controller type
        private static object workaround = typeof (OrderApiController);

        [SetUp]
        public void SetUp()
        {
            TestSetUp.InitializeEventStore();
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [Test]
        public void Api_responds_to_request_for_commands()
        {
            var response = new TestApi<Order>().GetClient().GetAsync("http://contoso.com/orders/help/commands").Result;

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public void Api_can_list_available_commands_for_its_domain_type()
        {
            var response = new TestApi<Order>().GetClient().GetAsync("http://contoso.com/orders/help/commands").Result;

            var json = response.Content.ReadAsStringAsync().Result;

            json.Should()
                .Contain(typeof (AddItem).Name)
                .And
                .Contain(typeof (ChangeCustomerInfo).Name)
                .And
                .Contain(typeof (RemoveItem).Name);
        }

        [Test]
        public void Optional_properties_types_are_displayed_in_an_informative_way()
        {
            var response = new TestApi<Order>().GetClient().GetAsync("http://contoso.com/orders/help/commands").Result;

            var jarray = JArray.Parse(response.Content.ReadAsStringAsync().Result);

            jarray.Single(j => j.Value<string>("CommandName") == "ChangeCustomerInfo")["Properties"]
                .Single(j => j.Value<string>("Name") == "Address")["Type"]
                .Value<string>()
                .Should().Be("Microsoft.Its.Domain.Optional(System.String)");
        }

        [Test]
        public void Command_help_includes_XML_summary()
        {
            var commandDoc = new CommandDocument(typeof (Place));

            commandDoc.Summary.Should().Be("Places the order.");
        }

        public class FakeAggregate : EventSourcedAggregate<FakeAggregate>
        {
            public FakeAggregate(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
            {
            }

            public class DocumentationTestEvent : Event<FakeAggregate>
            {
                public override void Update(FakeAggregate aggregate)
                {
                }
            }

            public FakeAggregate(Guid? id = null) : base(id)
            {
            }
        }

        [Test]
        public void Api_responds_to_request_for_help()
        {
            var response = new TestApi<Order>().GetClient().GetAsync("http://contoso.com/orders/help").Result;

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
