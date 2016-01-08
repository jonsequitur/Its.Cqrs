// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Microsoft.Its.Domain.Api.Tests.Infrastructure;

namespace Microsoft.Its.Domain.Api.Tests
{
    [TestFixture]
    public class DiagnosticsControllerTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            TestSetUp.InitializeEventStore();
        }

        [Ignore("Test not finished")]
        [Test]
        public void Related_events_are_available_via_diagnostics_endpoint()
        {
            // arrange
            var relatedId1 = Any.Guid();
            var relatedId2 = Any.Guid();
            var relatedId3 = Any.Guid();
            var relatedId4 = Any.Guid();
            var unrelatedId = Any.Guid();

            Console.WriteLine(new
            {
                relatedId1,
                relatedId2,
                relatedId3,
                relatedId4,
                unrelatedId
            }.ToLogString());

            using (var db = new EventStoreDbContext())
            {
                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId1,
                    SequenceNumber = i,
                    Body = new { relatedId2 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "one",
                    Type = "Event" + i.ToString()
                }));

                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId2,
                    SequenceNumber = i,
                    Body = new { relatedId3 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "two",
                    Type = "Event" + i.ToString()
                }));

                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId3,
                    SequenceNumber = i,
                    Body = new { relatedId4 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "three",
                    Type = "Event" + i.ToString()
                }));

                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId4,
                    SequenceNumber = i,
                    Body = new { }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "three",
                    Type = "Event" + i.ToString()
                }));

                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = unrelatedId,
                    SequenceNumber = i,
                    Body = new { }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "three",
                    Type = "Event" + i.ToString()
                }));

                db.SaveChanges();
            }

            var client = CreateClient();

            // act
            var response = client.GetAsync("http://contoso.com/api/events/related/" + relatedId1)
                .Result
                .ShouldSucceed()
                .JsonContent();

            Console.WriteLine(response);

            // assert
            //            events.Count().Should().Be(80);
            //            events.Should().Contain(e => e.AggregateId == relatedId1);
            //            events.Should().Contain(e => e.AggregateId == relatedId2);
            //            events.Should().Contain(e => e.AggregateId == relatedId3);
            //            events.Should().Contain(e => e.AggregateId == relatedId4);
            //            events.Should().NotContain(e => e.AggregateId == unrelatedId);

            // TODO: (Related_events_are_available_via_diagnostics_endpoint) write test
            Assert.Fail("Test not written yet.");
        }

        public HttpClient CreateClient()
        {
            var configuration = new HttpConfiguration();
            // TODO: (CreateClient)   .MapDiagnostics();
            var server = new HttpServer(configuration);
            var httpClient = new HttpClient(server);
            return httpClient;
        }
    }
}
