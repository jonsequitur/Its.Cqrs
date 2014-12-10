using System.Data.Entity.Migrations;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    using System;

    public partial class v0_9_0 : DbMigration
    {
        private string etag = "ETag";
        private const string eventsTable = "EventStore.Events";

        public override void Up()
        {
            AddColumn(eventsTable,
                      etag,
                      c => c.String(nullable: true, maxLength: 255));

            CreateIndex(eventsTable, etag);

            AlterColumn(eventsTable, "StreamName",
                        c => c.String(maxLength: 255));
            AlterColumn(eventsTable, "Type",
                        c => c.String(maxLength: 255));
            AlterColumn(eventsTable, "Actor",
                        c => c.String(maxLength: 255));

            CreateIndex(eventsTable, "StreamName");
            CreateIndex(eventsTable, "Type");
        }

        public override void Down()
        {
        }
    }
}