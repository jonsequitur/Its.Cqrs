// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    internal static class EventSourcedRepositoryExtensions
    {
        private static readonly MethodInfo createMethod = typeof (CommandFailed)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(m => m.Name == "Create");

        public static async Task<ScheduledCommandResult> ApplyScheduledCommand<TAggregate>(
            this IEventSourcedRepository<TAggregate> repository,
            IScheduledCommand<TAggregate> scheduled,
            Func<Task<bool>> verifyPrecondition = null)
            where TAggregate : class, IEventSourced
        {
            TAggregate aggregate = null;
            Exception exception = null;

            try
            {
                if (verifyPrecondition != null && !await verifyPrecondition())
                {
                    return await FailScheduledCommand(repository, scheduled);
                }

                aggregate = await repository.GetLatest(scheduled.AggregateId);

                if (aggregate == null)
                {
                    if (scheduled.Command is ConstructorCommand<TAggregate>)
                    {
                        var ctor = typeof (TAggregate).GetConstructor(new[] { scheduled.Command.GetType() });
                        aggregate = (TAggregate) ctor.Invoke(new[] { scheduled.Command });
                    }
                    else
                    {
                        // TODO: (ApplyScheduledCommand) this should probably be a different exception type.
                        throw new ConcurrencyException(
                            string.Format("No {0} was found with id {1} so the command could not be applied.",
                                          typeof (TAggregate).Name, scheduled.AggregateId),
                            new IEvent[] { scheduled });
                    }
                }
                else
                {
                    aggregate.Apply(scheduled.Command);
                }

                await repository.Save(aggregate);

                return new CommandSucceeded(scheduled);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            return await FailScheduledCommand(repository, scheduled, exception, aggregate);
        }

        private static async Task<ScheduledCommandResult> FailScheduledCommand<TAggregate>(
            IEventSourcedRepository<TAggregate> repository,
            IScheduledCommand<TAggregate> scheduled,
            Exception exception = null,
            TAggregate aggregate = null)
            where TAggregate : class, IEventSourced
        {
            var failure = (CommandFailed) createMethod
                                                        .MakeGenericMethod(scheduled.Command.GetType())
                                                        .Invoke(null, new object[] { scheduled.Command, scheduled, exception });

            var previousAttempts = scheduled.IfHas<int>(s => s.Metadata.NumberOfPreviousAttempts)
                                            .ElseDefault();

            failure.NumberOfPreviousAttempts = previousAttempts;

            if (aggregate != null)
            {
                // TODO: (FailScheduledCommand) refactor so that getting hold of the handler is simpler
                scheduled.Command
                         .IfTypeIs<Command<TAggregate>>()
                         .ThenDo(c =>
                         {
                             if (c.Handler != null)
                             {
                                 Task task = c.Handler
                                              .HandleScheduledCommandException((dynamic) aggregate,
                                                                               (dynamic) failure);
                                 task.Wait();
                             }
                         });

                if (!(exception is ConcurrencyException))
                {
                    try
                    {
                        await repository.Save(aggregate);
                    }
                    catch (Exception ex)
                    {
                        // TODO: (FailScheduledCommand) surface this more clearly
                        Trace.Write(ex);
                    }
                }
            }
            else
            {
                if (failure.NumberOfPreviousAttempts < 5)
                {
                    failure.Retry(TimeSpan.FromMinutes(failure.NumberOfPreviousAttempts + 1));
                }
            }

            return failure;
        }
    }
}
