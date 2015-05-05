using System.Data.Entity;
using System.Data.Entity.SqlServer;


namespace Microsoft.Its.Domain.Sql
{
    public class EfDbConfiguration : DbConfiguration
    {
        public EfDbConfiguration()
        {
            SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
        }
    }
}
