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
	[MigrationScope] [nvarchar](25) NOT NULL,
	[MigrationVersion] [nvarchar](25) NOT NULL,
	[Log] [nvarchar](max) NULL
)

SET NOCOUNT ON;

INSERT @migrations ([MigrationScope], [MigrationVersion], [Log]) 
VALUES (N'DbContext', N'0.0.0.0', N'Inserted by db project')

INSERT @migrations ([MigrationScope], [MigrationVersion], [Log]) 
VALUES (N'CommandSchedulerDbContext', N'0.13.8', N'Inserted by db project')

INSERT @migrations ([MigrationScope], [MigrationVersion], [Log]) 
VALUES (N'CommandSchedulerDbContext', N'0.14.0', N'Inserted by db project')

INSERT @migrations ([MigrationScope], [MigrationVersion], [Log]) 
VALUES (N'CommandSchedulerDbContext', N'0.14.11', N'Inserted by db project')

INSERT @migrations ([MigrationScope], [MigrationVersion], [Log]) 
VALUES (N'CommandSchedulerDbContext', N'0.14.12', N'Inserted by db project')

INSERT @migrations ([MigrationScope], [MigrationVersion], [Log]) 
VALUES (N'CommandSchedulerDbContext', N'0.15.3', N'Inserted by db project')

--------------------

merge [PocketMigrator].[AppliedMigrations] as target
using @migrations as source
on (target.[MigrationScope] = source.[MigrationScope] and target.[MigrationVersion] = source.[MigrationVersion])
when not matched then
   insert ([MigrationScope], [MigrationVersion], [Log], [AppliedDate])
   values (source.[MigrationScope], source.[MigrationVersion], source.[Log], getdate());
