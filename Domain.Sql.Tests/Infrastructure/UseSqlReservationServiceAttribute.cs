// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Tests;
using NUnit.Framework.Interfaces;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class UseSqlReservationServiceAttribute : DomainConfigurationAttribute
    {
        protected override void BeforeTest(ITest test, Configuration configuration) =>
            configuration.UseSqlReservationService(c => c.UseConnectionString(TestDatabases.ReservationService.ConnectionString));
    }
}