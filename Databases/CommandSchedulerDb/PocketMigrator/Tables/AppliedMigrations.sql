CREATE TABLE [PocketMigrator].[AppliedMigrations] (
    [Sequence]         BIGINT             IDENTITY (1, 1) NOT NULL,
    [MigrationScope]   NVARCHAR (25)      NOT NULL,
    [MigrationVersion] NVARCHAR (25)      NOT NULL,
    [Log]              NVARCHAR (MAX)     NULL,
    [AppliedDate]      DATETIMEOFFSET (7) NULL,
    CONSTRAINT [PK_Leases_1] PRIMARY KEY CLUSTERED ([MigrationScope] ASC, [MigrationVersion] ASC)
);

