using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class FakeEventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate> where TAggregate : class, IEventSourced
    {
        private readonly SqlEventSourcedRepository<TAggregate> innerRepository;

        public FakeEventSourcedRepository(SqlEventSourcedRepository<TAggregate> innerRepository)
        {
            this.innerRepository = innerRepository;
            OnSave = innerRepository.Save;
        }

        public Task<TAggregate> GetLatest(Guid aggregateId)
        {
            return innerRepository.GetLatest(aggregateId);
        }

        public Task<TAggregate> GetVersion(Guid aggregateId, long version)
        {
            return innerRepository.GetVersion(aggregateId, version);
        }

        public Task<TAggregate> GetAsOfDate(Guid aggregateId, DateTimeOffset asOfDate)
        {
            return innerRepository.GetAsOfDate(aggregateId, asOfDate);
        }

        public async Task Save(TAggregate aggregate)
        {
            await OnSave(aggregate);
        }

        public Task Refresh(TAggregate aggregate)
        {
            throw new NotImplementedException();
        }

        public Func<TAggregate, Task> OnSave;
    }
}