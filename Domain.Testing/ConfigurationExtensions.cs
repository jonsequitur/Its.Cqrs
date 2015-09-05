using System;
using System.Reactive.Linq;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Triggers commands scheduled on the <see cref="SqlCommandScheduler" /> when a virtual clock is advanced.
        /// </summary>
        /// <param name="configuration">A domain configuration instance.</param>
        /// <returns>The modified domain configuration instance.</returns>
        public static Configuration TriggerSqlCommandSchedulerWithVirtualClock(this Configuration configuration)
        {
            if (!configuration.IsUsingSqlCommandScheduling())
            {
                throw new InvalidOperationException("Only supported after configuring with UseSqlCommandScheduler.");
            }

            var scheduler = configuration.SqlCommandScheduler();

            var subscription = scheduler.Activity
                                        .OfType<CommandScheduled>()
                                        .Subscribe(scheduled =>
                                        {
                                            Clock.Current
                                                 .IfTypeIs<VirtualClock>()
                                                 .ThenDo(clock =>
                                                 {
                                                     clock.OnAdvanceTriggerSchedulerClock(scheduled.ClockName);
                                                 });
                                        });

            configuration.RegisterForDisposal(subscription);

            return configuration;
        }
    }
}