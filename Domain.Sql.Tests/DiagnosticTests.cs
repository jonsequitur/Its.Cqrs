// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [UseSqlEventStore]
    public class DiagnosticTests : EventStoreDbTest
    {
        [Test]
        public async Task Sensor_can_be_used_to_check_read_model_catchup_status()
        {
            Events.Write(1);

            CreateReadModelCatchup(Projector.Create<Order.Cancelled>(e => {})).Dispose();

            var result = Sensors.CatchupStatus().Result;

            ((long) result.LatestEventId).Should().BeGreaterOrEqualTo(0);
            ((int) result.ReadModels.Count).Should().BeGreaterOrEqualTo(0);
        }
    }
}