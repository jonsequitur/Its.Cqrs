// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class DiagnosticTests : EventStoreDbTest
    {
        [Test]
        public void Sensor_can_be_used_to_check_read_model_catchup_status()
        {
            var result = Sensors.CatchupStatus().Result;

            ((long) result.LatestEventId).Should().BeGreaterOrEqualTo(0);
            ((int) result.ReadModels.Count).Should().BeGreaterOrEqualTo(0);
        }
    }
}
