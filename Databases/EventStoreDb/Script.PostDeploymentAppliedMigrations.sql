/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
DECLARE @migrations TABLE
(
	[Sequence] [bigint] NOT NULL,
	[MigrationScope] [nvarchar](25) NOT NULL,
	[MigrationVersion] [nvarchar](25) NOT NULL,
	[Log] [nvarchar](max) NULL
)

SET NOCOUNT ON;

INSERT @migrations ([Sequence], [MigrationScope], [MigrationVersion], [Log]) VALUES (1, N'DbContext', N'0.0.0.0', N'Microsoft.Its.Domain.Sql.Migrations.ScriptBasedDbMigrator, Microsoft.Its.Domain.Sql, Version=0.14.0.0, Culture=neutral, PublicKeyToken=null

IF (NOT EXISTS (SELECT * FROM sys.schemas WHERE name = ''PocketMigrator'')) 
BEGIN
    EXEC (''CREATE SCHEMA [PocketMigrator]'')
END

IF object_id(''[PocketMigrator].[AppliedMigrations]'') IS NULL
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
END')

INSERT @migrations ([Sequence], [MigrationScope], [MigrationVersion], [Log]) VALUES (2, N'EventStoreDbContext', N'0.14.0', N'Microsoft.Its.Domain.Sql.Migrations.ScriptBasedDbMigrator, Microsoft.Its.Domain.Sql, Version=0.14.0.0, Culture=neutral, PublicKeyToken=null

if not exists(select * from sys.indexes where name=''IX_ETag'' and object_id = OBJECT_ID(''eventstore.events''))
begin
	CREATE NONCLUSTERED INDEX [IX_ETag] ON [EventStore].[Events]
	(
		[ETag] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists(select * from sys.indexes where name=''IX_StreamName'' and object_id = OBJECT_ID(''eventstore.events''))
begin
	CREATE NONCLUSTERED INDEX [IX_StreamName] ON [EventStore].[Events]
	(
		[StreamName] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists(select * from sys.indexes where name=''IX_Type'' and object_id = OBJECT_ID(''eventstore.events''))
begin
	CREATE NONCLUSTERED INDEX [IX_Type] ON [EventStore].[Events]
	(
		[Type] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists(select * from sys.indexes where name=''IX_Id_and_Type'' and object_id = OBJECT_ID(''eventstore.events''))
begin
	CREATE NONCLUSTERED INDEX [IX_Id_and_Type] ON [EventStore].[Events] 
	(
		[Id] ASC,
		[Type] ASC
	)
	INCLUDE ([StreamName])
end')

--------------------

SET IDENTITY_INSERT [PocketMigrator].[AppliedMigrations] ON 
merge [PocketMigrator].[AppliedMigrations] as target
using @migrations as source
on (target.[MigrationScope] = source.[MigrationScope] and target.[MigrationVersion] = source.[MigrationVersion])
when not matched then
   insert ([Sequence], [MigrationScope], [MigrationVersion], [Log], [AppliedDate])
   values (source.[Sequence], source.[MigrationScope], source.[MigrationVersion], source.[Log], getdate());

SET IDENTITY_INSERT [PocketMigrator].[AppliedMigrations] OFF