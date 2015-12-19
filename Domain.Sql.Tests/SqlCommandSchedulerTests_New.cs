using System;
using System.Linq;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [Category("Command scheduling")]
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
                .UseSqlStorageForScheduledCommands()
                .TraceScheduledCommands();
        }
    }
}