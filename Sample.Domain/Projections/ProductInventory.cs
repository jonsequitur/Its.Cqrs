using Microsoft.Its.Log.Instrumentation;

namespace Sample.Domain.Projections
{
    public class ProductInventory
    {
        static ProductInventory()
        {
            Formatter<ProductInventory>.RegisterForAllMembers();
        }

        public string ProductName { get; set; }
        public int QuantityInStock { get; set; }
        public int QuantityReserved { get; set; }
    }
}