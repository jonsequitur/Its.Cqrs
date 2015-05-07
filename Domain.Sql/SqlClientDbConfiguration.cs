using System.Data.Entity;
using System.Data.Entity.SqlServer;

namespace Microsoft.Its.Domain.Sql
{
    public class SqlClientDbConfiguration : DbConfiguration
    {
        public SqlClientDbConfiguration()
        {
            SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
        }
    }
}
