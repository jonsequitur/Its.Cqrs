// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql.Tests
{
    public static class TestDatabases
    {
        public static class CommandScheduler
        {
            public static string ConnectionString { get; } =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsCommandScheduler";
        }

        public static class EventStore
        {
            public static string ConnectionString { get; } =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsEventStore";
        }

        public static class ReadModels
        {
            public static string ConnectionString { get; } =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsReadModels";
        }

        public static class ReservationService
        {
            public static string ConnectionString { get; } =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsReservationService";
        }

        public static EventStoreDbContext EventStoreDbContext()
        {
            return new EventStoreDbContext(EventStore.ConnectionString);
        }

        public static ReadModelDbContext ReadModelDbContext()
        {
            return new ReadModelDbContext(ReadModels.ConnectionString);
        }
    }
}