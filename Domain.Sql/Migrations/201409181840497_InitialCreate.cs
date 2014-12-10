using System;
using System.Data.Entity.Migrations;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable("EventStore.Events",
                        c => new
                        {
                            AggregateId = c.Guid(),
                            SequenceNumber = c.Long(),
                            Id = c.Long(identity: true),
                            StreamName = c.String(),
                            Type = c.String(),
                            UtcTime = c.DateTime(),
                            Actor = c.String(nullable: true),
                            Body = c.String(nullable: true),
                        })
                .PrimaryKey(c => new { c.AggregateId, c.SequenceNumber });
        }

        public override void Down()
        {
        }
    }
}