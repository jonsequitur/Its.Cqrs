using System;
using System.Diagnostics;
using Microsoft.Its.Domain.Sql;

namespace Microsoft.Its.Domain.Api.Tests.Infrastructure
{
    [DebuggerStepThrough]
    public class AnonymousProjector<TEvent> : IUpdateProjectionWhen<TEvent> where TEvent : IEvent
    {
        private readonly Action<TEvent> action;

        public AnonymousProjector(Action<TEvent> action)
        {
            this.action = action;
        }

        public void UpdateProjection(TEvent @event)
        {
            using (var work = this.Update())
            {
                action(@event);
                work.VoteCommit();
            }
        }
    }
}