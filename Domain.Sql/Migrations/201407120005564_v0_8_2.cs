// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql.CommandScheduler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class v0_8_2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("Events.ReadModelInfo", "InitialCatchupStartTime", c => c.DateTimeOffset());
            AddColumn("Events.ReadModelInfo", "InitialCatchupEvents", c => c.Long(nullable: false));
            AddColumn("Events.ReadModelInfo", "InitialCatchupEndTime", c => c.DateTimeOffset());
            AddColumn("Events.ReadModelInfo", "BatchRemainingEvents", c => c.Long(nullable: false));
            AddColumn("Events.ReadModelInfo", "BatchStartTime", c => c.DateTimeOffset());
            AddColumn("Events.ReadModelInfo", "BatchTotalEvents", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("Events.ReadModelInfo", "BatchTotalEvents");
            DropColumn("Events.ReadModelInfo", "BatchStartTime");
            DropColumn("Events.ReadModelInfo", "BatchRemainingEvents");
            DropColumn("Events.ReadModelInfo", "InitialCatchupEndTime");
            DropColumn("Events.ReadModelInfo", "InitialCatchupEvents");
            DropColumn("Events.ReadModelInfo", "InitialCatchupStartTime");
        }
    }
}
