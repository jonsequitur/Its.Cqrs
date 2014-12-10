using Its.Log.Instrumentation;

namespace Sample.Domain.Projections
{
    public class OrderTally
    {
        static OrderTally()
        {
            Formatter<OrderTally>.RegisterForAllMembers();
        }

        public string Status { get; set; }

        public int Count { get; set; }

        public enum OrderStatus
        {
            Pending,
            Canceled,
            Delivered
        }
    }
}