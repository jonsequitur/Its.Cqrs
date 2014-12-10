namespace Microsoft.Its.Domain.Sql.Tests
{
    public class ReadModels1DbContext : ReadModelDbContext
    {
        public ReadModels1DbContext() : base(@"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsReadModels1DbContext")
        {
        }
    }
}