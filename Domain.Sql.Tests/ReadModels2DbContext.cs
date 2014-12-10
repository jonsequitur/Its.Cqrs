namespace Microsoft.Its.Domain.Sql.Tests
{
    public class ReadModels2DbContext : ReadModelDbContext
    {
        public ReadModels2DbContext() : base(@"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsReadModels2DbContext")
        {
        }
    }
}