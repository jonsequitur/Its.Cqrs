using System;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Testing;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SynchronousSqlReservationServiceTests : SqlReservationServiceTests
    {
        protected override void Configure(Configuration configuration, Action onSave = null)
        {
            configuration.UseSqlReservationService()
                         .UseSqlEventStore()
                         .UseEventBus(new FakeEventBus());
        }

        private class ReservationServiceShim : IReservationService
        {
            private readonly ISynchronousReservationService synchronousReservationService;

            public ReservationServiceShim(ISynchronousReservationService synchronousReservationService)
            {
                if (synchronousReservationService == null)
                {
                    throw new ArgumentNullException("synchronousReservationService");
                }
                this.synchronousReservationService = synchronousReservationService;
            }

            public Task<bool> Reserve(string value, string scope, string ownerToken, TimeSpan? lease = null)
            {
                return Task.Run(() => synchronousReservationService.Reserve(value, scope, ownerToken, lease));
            }

            public Task<bool> Confirm(string value, string scope, string ownerToken)
            {
                return Task.Run(() => synchronousReservationService.Confirm(value, scope, ownerToken));
            }

            public Task<bool> Cancel(string value, string scope, string ownerToken)
            {
                return Task.Run(() => synchronousReservationService.Cancel(value, scope, ownerToken));
            }

            public Task<string> ReserveAny(string scope, string ownerToken, TimeSpan? lease = null, string confirmationToken = null)
            {
                return Task.Run(() => synchronousReservationService.ReserveAny(scope, ownerToken, lease, confirmationToken));
            }
        }
    }
}