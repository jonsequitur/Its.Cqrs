using System;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.EventStore
{
    public static class EventStreamExtensions
    {
        public static long NextVersion(
            this IEventStream stream,
            string id)
        {
            return stream.Latest(id)
                         .IfNotNull()
                         .Then(r => r.SequenceNumber + 1)
                         .Else(() => 1);
        }
    }
}