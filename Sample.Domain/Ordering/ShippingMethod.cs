namespace Sample.Domain.Ordering
{
    public class ShippingMethod : IDeliveryMethod
    {
        public string Carrier { get; set; }
        public string ServiceMethod { get; set; }
        public decimal Price { get; set; }
    }
}