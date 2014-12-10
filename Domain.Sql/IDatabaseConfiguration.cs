using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql
{
    public interface IDatabaseConfiguration<in TContext> where TContext : DbContext
    {
        void ConfigureDatabase(TContext context);
    }
}