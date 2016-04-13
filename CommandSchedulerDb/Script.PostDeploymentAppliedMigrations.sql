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

INSERT @migrations ([Sequence], [MigrationScope], [MigrationVersion], [Log]) 
VALUES (1, N'DbContext', N'0.0.0.0', N'Microsoft.Its.Domain.Sql.Migrations.ScriptBasedDbMigrator, Microsoft.Its.Domain.Sql, Version=0.14.0.0, Culture=neutral, PublicKeyToken=null

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

INSERT @migrations ([Sequence], [MigrationScope], [MigrationVersion], [Log]) 
VALUES (2, N'CommandSchedulerDbContext', N'0.13.8', N'Microsoft.Its.Domain.Sql.Migrations.ScriptBasedDbMigrator, Microsoft.Its.Domain.Sql, Version=0.14.0.0, Culture=neutral, PublicKeyToken=null

IF NOT EXISTS (SELECT Id FROM [Scheduler].[Clock] 
			   WHERE Name LIKE ''default'')
  BEGIN
	INSERT INTO [Scheduler].[Clock] 
	(Name, StartTime, UtcNow)
	VALUES 
	(''default'', GetDate(), GetDate())
  END')

INSERT @migrations ([Sequence], [MigrationScope], [MigrationVersion], [Log]) 
VALUES (3, N'CommandSchedulerDbContext', N'0.14.0', N'Microsoft.Its.Domain.Sql.Migrations.ScriptBasedDbMigrator, Microsoft.Its.Domain.Sql, Version=0.14.0.0, Culture=neutral, PublicKeyToken=null

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(''Scheduler.ETag''))
BEGIN
     CREATE TABLE [Scheduler].[ETag](
		[Id] [bigint] IDENTITY(1,1) not null,
        [Scope] [nvarchar](50) NOT NULL,
        [ETagValue] [nvarchar](50) NOT NULL,     
        [CreatedDomainTime] [datetimeoffset](7) NOT NULL,
        [CreatedRealTime] [datetimeoffset](7) NOT NULL
     CONSTRAINT [PK_Id] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]

	CREATE UNIQUE NONCLUSTERED INDEX [IX_Scope_and_ETag] ON [Scheduler].[ETag]
    (
    	[Scope] ASC,
    	[ETagValue] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
END')

INSERT @migrations ([Sequence], [MigrationScope], [MigrationVersion], [Log]) 
VALUES (4, N'CommandSchedulerDbContext', N'0.14.11', N'Microsoft.Its.Domain.Sql.Migrations.ScriptBasedDbMigrator, Microsoft.Its.Domain.Sql, Version=0.14.0.0, Culture=neutral, PublicKeyToken=null

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name=''IX_Scope_and_ETag'' AND object_id = OBJECT_ID(''Scheduler.ETag''))
BEGIN
	CREATE UNIQUE NONCLUSTERED INDEX [IX_Scope_and_ETag] ON [Scheduler].[ETag]
    (
    	[Scope] ASC,
    	[ETagValue] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
END

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name=''IX_Temp'' AND object_id = OBJECT_ID(''Scheduler.ScheduledCommand''))
BEGIN
	CREATE UNIQUE NONCLUSTERED INDEX [IX_Temp] on [Scheduler].[ScheduledCommand]
	(
	[AggregateId] ASC,
	[SequenceNumber] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

	ALTER TABLE [Scheduler].[Error] DROP CONSTRAINT [FK_Scheduler.Error_Scheduler.ScheduledCommand_ScheduledCommand_AggregateId_ScheduledCommand_SequenceNumber]

	ALTER TABLE [Scheduler].[ScheduledCommand] DROP CONSTRAINT [PK_Scheduler.ScheduledCommand]

	ALTER TABLE [Scheduler].[ScheduledCommand] ADD  CONSTRAINT [PK_Scheduler.ScheduledCommand] PRIMARY KEY NONCLUSTERED 
	(
	[AggregateId] ASC,
	[SequenceNumber] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

	DROP INDEX [IX_Temp] on [Scheduler].[ScheduledCommand]
	
	CREATE CLUSTERED INDEX [IX_AppliedTime_Clock_Id_FinalAttemptTime_DueTime] ON 
	[Scheduler].[ScheduledCommand] 
		([AppliedTime], 
		 [Clock_Id], 
		 [FinalAttemptTime], 
		 [DueTime]) 
	WITH (ONLINE = OFF)
END')


--------------------

SET IDENTITY_INSERT [PocketMigrator].[AppliedMigrations] ON 
merge [PocketMigrator].[AppliedMigrations] as target
using @migrations as source
on (target.[MigrationScope] = source.[MigrationScope] and target.[MigrationVersion] = source.[MigrationVersion])
when not matched then
   insert ([Sequence], [MigrationScope], [MigrationVersion], [Log], [AppliedDate])
   values (source.[Sequence], source.[MigrationScope], source.[MigrationVersion], source.[Log], getdate());

SET IDENTITY_INSERT [PocketMigrator].[AppliedMigrations] OFF