using Microsoft.Its.Domain.Sql.CommandScheduler;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public static class TestDatabases
    {
        public static void SetConnectionStrings()
        {
            EventStoreDbContext.NameOrConnectionString = EventStore.ConnectionString;

            ReadModelDbContext.NameOrConnectionString = ReadModels.ConnectionString;

            CommandSchedulerDbContext.NameOrConnectionString = CommandScheduler.ConnectionString;
        }

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
    }
}