// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Sql;

namespace Microsoft.Its.Domain.Api.Tests
{
    public static class TestSetUp
    {
        public static void EnsureEventStoreIsInitialized()
        {
            EventStoreDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsEventStore";
        }
    }
}