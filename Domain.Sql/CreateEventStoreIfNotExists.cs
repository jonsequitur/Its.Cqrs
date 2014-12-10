using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    [Obsolete("Please use EventStoreDatabaseInitializer<T> instead, which supports migrations and multiple context types.")]
    public class CreateEventStoreIfNotExists : CreateDatabaseIfNotExists<EventStoreDbContext>
    {
        protected override void Seed(EventStoreDbContext context)
        {
            OnSeed.IfNotNull()
                  .ThenDo(seed => seed(context));
            base.Seed(context);
        }

        public static Action<EventStoreDbContext> OnSeed = context => { };
    }
}