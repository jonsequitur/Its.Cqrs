using System;
using System.Threading.Tasks;

namespace Sample.Domain.Ordering
{
    public class CreditCardPaymentGateway : IPaymentService
    {
        private readonly decimal chargeLimit;

        public CreditCardPaymentGateway(decimal chargeLimit = decimal.MaxValue)
        {
            this.chargeLimit = chargeLimit;
        }

        public async Task<PaymentId> Charge(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be at least 0.");
            }

            return await Task.Run(() => new PaymentId(Guid.NewGuid().ToString()));
        }
    }
}