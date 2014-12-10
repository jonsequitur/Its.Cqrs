using Microsoft.Its.Domain;
using Its.Validation;

namespace Sample.Domain.Ordering.Commands
{
    public class ChangeCustomerInfo : Command<Order>
    {
        public string CustomerName { get; set; }

        public Optional<string> Address { get; set; }
        
        public Optional<string> PostalCode { get; set; }

        public Optional<string> RegionOrCountry { get; set; }

        public Optional<string> PhoneNumber { get; set; }

        public override IValidationRule<Order> Validator
        {
            get
            {
                return Order.NotFulfilled;
            }
        }
    }
}