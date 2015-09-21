// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Diagnostics;
using Its.Log.Instrumentation;
using System.Linq;
using Microsoft.Its.Domain.Sql;
using TraceListener = Its.Log.Instrumentation.TraceListener;

namespace Microsoft.Its.Domain.Tests.Infrastructure
{
    public static class Logging
    {
        private static readonly TraceListener itsLogListener = new TraceListener();

        public static void Configure()
        {
            if (!Trace.Listeners.Contains(itsLogListener))
            {
                Trace.Listeners.Clear();
                Trace.Listeners.Add(itsLogListener);

                Log.EntryPosted += (o, e) => Console.WriteLine(e.LogEntry.ToLogString());

                Formatter.RecursionLimit = 12;

                Formatter<LogEntry>.Register((entry, writer) =>
                {
                    if (entry.CallingType != null && entry.CallingMethod != null)
                    {
                        writer.Write("[{0}.{1}] {2}",
                                     entry.CallingType,
                                     entry.CallingMethod,
                                     entry.Subject.ToLogString());
                    }
                    else
                    {
                        writer.WriteLine(entry.Subject.ToLogString());
                    }
                    writer.WriteLine();
                });

                Formatter.AutoGenerateForType = type =>
                {
                    return new[]
                    {
                        typeof (ICommand),
                        typeof (IEvent),
                        typeof (IScheduledCommand),
                        typeof (ICommandSchedulerActivity)
                    }.Any(t => t.IsAssignableFrom(type));
                };

                Formatter<ReadModelInfo>.RegisterForAllMembers();
                Formatter<CommandFailed>.RegisterForAllMembers();
            }
        }
    }
}