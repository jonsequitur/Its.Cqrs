using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [SetUpFixture]
    internal class SetUpDbConfiguration
    {
        [SetUp]
        public void SetUp()
        {
            DbConfiguration.SetConfiguration(new TestDbConfiguration());
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("SetUpFixture.TearDown");
        }
    }
    
    public class TestDbConfiguration : DbConfiguration
    {
        public TestDbConfiguration()
        {
            SetExecutionStrategy("System.Data.SqlClient",
                                 () => new SqlAzureExecutionStrategy());
        }

        public static bool UseSqlAzureExecutionStrategy { get; set; }
    }
}