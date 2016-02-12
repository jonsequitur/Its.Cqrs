// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public abstract class SqlCommandSchedulerTests
    {
        public abstract Task When_a_clock_is_advanced_its_associated_commands_are_triggered();

        protected abstract void ConfigureScheduler(Configuration configuration);
    }
}