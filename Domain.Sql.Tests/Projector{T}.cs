using System;
using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class Projector<T> :
        IEntityModelProjector,
        IUpdateProjectionWhen<T>
        where T : IEvent
    {
        private readonly Func<DbContext> createDbContext;

        public Projector(Func<DbContext> createDbContext)
        {
            if (createDbContext == null)
            {
                throw new ArgumentNullException("createDbContext");
            }
            this.createDbContext = createDbContext;
        }

        public int CallCount { get; set; }

        public void UpdateProjection(T @event)
        {
            using (var work = this.Update())
            {
                CallCount++;
                OnUpdate(work, @event);
                work.VoteCommit();
            }
        }

        public Action<UnitOfWork<ReadModelUpdate>, T> OnUpdate = (work, @event) => { };

        public DbContext CreateDbContext()
        {
            return createDbContext();
        }
    }
}