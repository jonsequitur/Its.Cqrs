IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID('Scheduler.ETag'))
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
    )
    )

	CREATE UNIQUE NONCLUSTERED INDEX [IX_Scope_and_ETag] ON [Scheduler].[ETag]
    (
    	[Scope] ASC,
    	[ETagValue] ASC
    )
END
