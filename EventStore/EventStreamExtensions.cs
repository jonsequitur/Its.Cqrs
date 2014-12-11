using System;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.EventStore
{
    public static class EventStreamExtensions
    {
        public static async Task<long> NextVersion(
            this IEventStream stream,
            string id)
        {
            var latest = await stream.Latest(id);
            
            return latest.IfNotNull()
                         .Then(r => r.SequenceNumber + 1)
                         .Else(() => 1);
        }
    }
}