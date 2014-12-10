using System;
using System.Data.Entity;
using System.Linq;

namespace Microsoft.Its.Domain.Sql
{
    public static class EventBusExtensions
    {
        /// <summary>
        /// Reports event handling errors via the specified database.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="db">The database.</param>
        /// <returns></returns>
        public static IDisposable ReportErrorsToDatabase(this IEventBus bus, Func<DbContext> db)
        {
            return bus.Errors.Subscribe(e => ReadModelUpdate.ReportFailure(e, db));
        }
    }
}