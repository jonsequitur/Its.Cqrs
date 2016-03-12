// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    internal class CommandSchedulerPipelineTraceInitializer : CommandSchedulerPipelineInitializer
    {
        private Action<IScheduledCommand> onScheduling;

        private Action<IScheduledCommand> onScheduled;

        private Action<IScheduledCommand> onDelivering;

        private Action<IScheduledCommand> onDelivered;

        protected override void InitializeFor<TAggregate>(Configuration configuration)
        {
            ScheduledCommandInterceptor<TAggregate> schedule = null;
            if (onScheduling != null || onScheduled != null)
            {
                onScheduling = onScheduling ?? delegate { };
                onScheduled = onScheduled ?? delegate { };

                schedule = async (cmd, next) =>
                {
                    onScheduling(cmd);
                    await next(cmd);
                    onScheduled(cmd);
                };
            }

            ScheduledCommandInterceptor<TAggregate> deliver = null;
            if (onDelivering != null || onDelivered != null)
            {
                onDelivering = onDelivering ?? delegate { };
                onDelivered = onDelivered ?? delegate { };

                deliver = async (cmd, next) =>
                {
                    onDelivering(cmd);
                    await next(cmd);
                    onDelivered(cmd);
                };
            }

            configuration.AddToCommandSchedulerPipeline(
                schedule: schedule,
                deliver: deliver);
        }

        public void OnScheduling(Action<IScheduledCommand> action)
        {
            if (action != null)
            {
                onScheduling = action;
            }
        }

        public void OnScheduled(Action<IScheduledCommand> action)
        {
            if (action != null)
            {
                onScheduled = action;
            }
        }

        public void OnDelivering(Action<IScheduledCommand> action)
        {
            if (action != null)
            {
                onDelivering = action;
            }
        }

        public void OnDelivered(Action<IScheduledCommand> action)
        {
            if (action != null)
            {
                onDelivered = action;
            }
        }

        protected internal override string GetKeyIndicatingInitialized()
        {
            var hashCodes = new[]
            {
                onScheduling,
                onScheduled,
                onDelivering,
                onDelivered
            }
                .Where(d => d != null)
                .Select(d => d.Method.GetHashCode().ToString()).ToDelimitedString("/");

            var key = $"{base.GetKeyIndicatingInitialized()} ({hashCodes})";

            return key;
        }
    }
}