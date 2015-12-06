// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    internal class CommandSchedulerPipelineTraceInitializer : SchedulerPipelineInitializer
    {
        private Action<IScheduledCommand> onScheduling = cmd =>
            Trace.WriteLine("[Scheduling] @" + Clock.Now() + ": " + cmd);

        private Action<IScheduledCommand> onScheduled = cmd =>
            Trace.WriteLine("[Scheduled] @" + Clock.Now() + ": " + cmd);

        private Action<IScheduledCommand> onDelivering = cmd =>
            Trace.WriteLine("[Delivering] @" + Clock.Now() + ": " + cmd);

        private Action<IScheduledCommand> onDelivered = cmd =>
            Trace.WriteLine("[Delivered] @" + Clock.Now() + ": " + cmd);

        protected override void InitializeFor<TAggregate>(Configuration configuration)
        {
            configuration.AddToCommandSchedulerPipeline<TAggregate>(
                schedule: async (cmd, next) =>
                {
                    onScheduling(cmd);
                    await next(cmd);
                    onScheduled(cmd);
                },
                deliver: async (cmd, next) =>
                {
                    onDelivering(cmd);
                    await next(cmd);
                    onDelivered(cmd);
                });
        }

        public void OnScheduling(Action<IScheduledCommand> action)
        {
            if (action != null)
            {
                this.onScheduling = action;
            }
        }

        public void OnScheduled(Action<IScheduledCommand> action)
        {
            if (action != null)
            {
                this.onScheduled = action;
            }
        }

        public void OnDelivering(Action<IScheduledCommand> action)
        {
            if (action != null)
            {
                this.onDelivering = action;
            }
        }

        public void OnDelivered(Action<IScheduledCommand> action)
        {
            if (action != null)
            {
                this.onDelivered = action;
            }
        }
    }
}