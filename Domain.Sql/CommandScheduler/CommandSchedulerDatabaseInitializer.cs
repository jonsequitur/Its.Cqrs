// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class CommandSchedulerDatabaseInitializer : CreateAndMigrate<CommandSchedulerDbContext>
    {
        protected override bool ShouldRebuildDatabase(CommandSchedulerDbContext context, Version latestVersion)
        {
            return false;
        }
    }
}