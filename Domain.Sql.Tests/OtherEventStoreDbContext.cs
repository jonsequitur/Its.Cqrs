namespace Microsoft.Its.Domain.Sql.Tests
{
    public class OtherEventStoreDbContext : EventStoreDbContext
    {
        public OtherEventStoreDbContext() : base(@"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsCatchupTestsOtherEventStore")
        {
        }
    }
}