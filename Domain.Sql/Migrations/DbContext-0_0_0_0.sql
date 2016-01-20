IF (NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'PocketMigrator')) 
BEGIN
    EXEC ('CREATE SCHEMA [PocketMigrator]')
END

IF object_id('[PocketMigrator].[AppliedMigrations]') IS NULL
BEGIN
    CREATE TABLE [PocketMigrator].[AppliedMigrations](
		[Sequence] [bigint] IDENTITY(1,1) NOT NULL,
        [MigrationScope] [nvarchar](25) NOT NULL,
        [MigrationVersion] [nvarchar](25) NOT NULL,
        [Log] [nvarchar](max) NULL,
        [AppliedDate] [datetimeoffset](7) NULL
    
	CONSTRAINT [PK_Leases_1] PRIMARY KEY CLUSTERED 
    (
        [MigrationScope] ASC,
        [MigrationVersion] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
END

