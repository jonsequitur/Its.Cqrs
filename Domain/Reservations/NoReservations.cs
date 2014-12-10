using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal class NoReservations : IReservationService
    {
        public Task<bool> Reserve(string value, string scope, string ownerToken, TimeSpan? lease = null)
        {
            throw new InvalidOperationException("No IReservationService has been configured.");
        }

        public Task<bool> Confirm(string value, string scope, string ownerToken)
        {
            throw new InvalidOperationException("No IReservationService has been configured.");
        }

        public Task<bool> Cancel(string value, string scope, string ownerToken)
        {
            throw new InvalidOperationException("No IReservationService has been configured.");
        }

        public Task<string> ReserveAny(string scope, string ownerToken, TimeSpan? lease = null, string confirmationToken = null)
        {
            throw new InvalidOperationException("No IReservationService has been configured.");
        }

        public static IReservationService Instance = new NoReservations();
    }
}