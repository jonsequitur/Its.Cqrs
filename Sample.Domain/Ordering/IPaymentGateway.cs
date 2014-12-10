using System;
using System.Threading.Tasks;

namespace Sample.Domain.Ordering
{
    public interface IPaymentService
    {
        Task<PaymentId> Charge(decimal amount);
    }
}