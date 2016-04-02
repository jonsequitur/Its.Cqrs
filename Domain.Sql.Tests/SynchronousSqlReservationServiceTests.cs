// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Testing;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SynchronousSqlReservationServiceTests : SqlReservationServiceTests
    {
        protected override void Configure(Configuration configuration, Action onSave = null)
        {
            configuration.UseSqlReservationService()
                         .UseSqlEventStore()
                         .UseEventBus(new FakeEventBus());
        }
    }
}