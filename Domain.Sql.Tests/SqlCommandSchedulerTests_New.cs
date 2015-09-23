using System;
using System.Linq;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using NUnit.Framework;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SqlCommandSchedulerTests_New : SqlCommandSchedulerTests
    {
        protected override void ConfigureScheduler(Configuration configuration)
        {
            configuration.Container.Register<SqlCommandScheduler>(c =>
            {
                throw new NotSupportedException("SqlCommandScheduler (legacy) is disabled");
                return null;
            });

            configuration
                .UseDependency<GetClockName>(c => e => clockName)
                .UseSqlStorageForScheduledCommands();
        }
    }
}