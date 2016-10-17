// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity.Migrations;

namespace Microsoft.Its.Domain.Sql.CommandScheduler.Migrations
{
    /// <summary>
    /// A database migration.
    /// </summary>
    /// <seealso cref="System.Data.Entity.Migrations.DbMigration" />
    /// <seealso cref="System.Data.Entity.Migrations.Infrastructure.IMigrationMetadata" />
    public partial class v0_6_11 : DbMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            CreateTable(
                "Scheduler.Clock",
                c => new
                {
                    Id = c.Int(nullable: false,
                        identity: true),
                    Name = c.String(nullable: false,
                        maxLength: 128),
                    StartTime = c.DateTimeOffset(nullable: false),
                    UtcNow = c.DateTimeOffset(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "Scheduler.ClockMapping",
                c => new
                {
                    Id = c.Long(nullable: false,
                        identity: true),
                    Value = c.String(nullable: false,
                        maxLength: 128),
                    Clock_Id = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("Scheduler.Clock",
                    t => t.Clock_Id,
                    cascadeDelete: true)
                .Index(t => t.Clock_Id);

            CreateTable(
                "Scheduler.ScheduledCommand",
                c => new
                {
                    AggregateId = c.Guid(nullable: false),
                    SequenceNumber = c.Long(nullable: false),
                    AggregateType = c.String(),
                    CreatedTime = c.DateTimeOffset(nullable: false),
                    DueTime = c.DateTimeOffset(),
                    AppliedTime = c.DateTimeOffset(),
                    FinalAttemptTime = c.DateTimeOffset(),
                    SerializedCommand = c.String(nullable: false),
                    Attempts = c.Int(nullable: false),
                    Clock_Id = c.Int(nullable: false),
                })
                .PrimaryKey(t => new
                {
                    t.AggregateId,
                    t.SequenceNumber
                })
                .ForeignKey("Scheduler.Clock",
                    t => t.Clock_Id,
                    cascadeDelete: true)
                .Index(t => t.Clock_Id);

            CreateTable(
                "Scheduler.Error",
                c => new
                {
                    Id = c.Long(nullable: false,
                        identity: true),
                    Error = c.String(nullable: false),
                    ScheduledCommand_AggregateId = c.Guid(nullable: false),
                    ScheduledCommand_SequenceNumber = c.Long(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("Scheduler.ScheduledCommand",
                    t => new
                    {
                        t.ScheduledCommand_AggregateId,
                        t.ScheduledCommand_SequenceNumber
                    },
                    cascadeDelete: true)
                .Index(t => new
                {
                    t.ScheduledCommand_AggregateId,
                    t.ScheduledCommand_SequenceNumber
                });

            CreateTable(
                "Events.ReadModelInfo",
                c => new
                {
                    Name = c.String(nullable: false,
                        maxLength: 256),
                    LastUpdated = c.DateTimeOffset(),
                    CurrentAsOfEventId = c.Long(nullable: false),
                    FailedOnEventId = c.Long(),
                    Error = c.String(),
                    LatencyInMilliseconds = c.Double(nullable: false),
                })
                .PrimaryKey(t => t.Name);

            CreateTable(
                "Events.EventHandlingErrors",
                c => new
                {
                    Id = c.Long(nullable: false,
                        identity: true),
                    Actor = c.String(),
                    Handler = c.String(),
                    SequenceNumber = c.Long(nullable: false),
                    AggregateId = c.Guid(nullable: false),
                    StreamName = c.String(),
                    EventTypeName = c.String(),
                    UtcTime = c.DateTimeOffset(nullable: false),
                    SerializedEvent = c.String(),
                    Error = c.String(),
                    OriginalId = c.Long(),
                })
                .PrimaryKey(t => t.Id);

            CreateIndex("Scheduler.Clock","Name",unique: true);
            CreateIndex("Scheduler.ClockMapping","Value",unique: true);
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
        }
    }
}
