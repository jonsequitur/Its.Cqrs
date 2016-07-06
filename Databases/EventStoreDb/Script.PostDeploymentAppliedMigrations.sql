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
VALUES (1, N'DbContext', N'0.0.0.0', N'Inserted by db project')

INSERT @migrations ([Sequence], [MigrationScope], [MigrationVersion], [Log]) 
VALUES (2, N'EventStoreDbContext', N'0.14.0', N'Inserted by db project')

--------------------

SET IDENTITY_INSERT [PocketMigrator].[AppliedMigrations] ON 
merge [PocketMigrator].[AppliedMigrations] as target
using @migrations as source
on (target.[MigrationScope] = source.[MigrationScope] and target.[MigrationVersion] = source.[MigrationVersion])
when not matched then
   insert ([Sequence], [MigrationScope], [MigrationVersion], [Log], [AppliedDate])
   values (source.[Sequence], source.[MigrationScope], source.[MigrationVersion], source.[Log], getdate());

SET IDENTITY_INSERT [PocketMigrator].[AppliedMigrations] OFF