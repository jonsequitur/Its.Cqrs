// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NUnit.Framework.Interfaces;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class UseSqlStorageForScheduledCommandsAttribute : DomainConfigurationAttribute
    {
        protected override void BeforeTest(ITest test, Configuration configuration)
        {
            var clockName = Any.CamelCaseName();

            configuration
                .UseInMemoryCommandTargetStore()
                .UseSqlStorageForScheduledCommands(c => c.UseConnectionString(TestDatabases.CommandScheduler.ConnectionString))
                .UseDependency<GetClockName>(c => _ => clockName);

            configuration
                .SchedulerClockRepository()
                .CreateClock(clockName, Clock.Now());
        }
    }
}