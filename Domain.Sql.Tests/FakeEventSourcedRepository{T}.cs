using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class FakeEventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate>
        where TAggregate : class, IEventSourced
    {
        private readonly IEventSourcedRepository<TAggregate> innerRepository;

        public FakeEventSourcedRepository(IEventSourcedRepository<TAggregate> innerRepository)
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
            return innerRepository.Refresh(aggregate);
        }

        public Func<TAggregate, Task> OnSave;

        public async Task<TAggregate> Get(string id)
        {
            return await innerRepository.Get(id);
        }

        public async Task Put(TAggregate aggregate)
        {
            await Save(aggregate);
        }
    }
}