// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using NUnit.Framework;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class DiagnosticTests : EventStoreDbTest
    {
        [SetUp]
        public void SetUp()
        {
            Sensors.GetEventStoreDbContext = () => EventStoreDbContext();
        }

        [Test]
        public void Sensor_can_be_used_to_check_read_model_catchup_status()
        {
            var result = Sensors.CatchupStatus().Result;

            ((long) result.LatestEventId).Should().BeGreaterOrEqualTo(0);
            ((int) result.ReadModels.Count).Should().BeGreaterOrEqualTo(0);
        }
    }
}
