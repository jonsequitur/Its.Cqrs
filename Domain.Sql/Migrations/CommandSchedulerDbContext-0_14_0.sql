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
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]

	CREATE UNIQUE NONCLUSTERED INDEX [IX_Scope_and_ETag] ON [Scheduler].[ETag]
    (
    	[Scope] ASC,
    	[ETagValue] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
END
