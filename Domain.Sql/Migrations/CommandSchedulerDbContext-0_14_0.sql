if not exists(select * from sys.tables where object_id = OBJECT_ID('Scheduler.ETag'))
begin
     CREATE TABLE [Scheduler].[ETag](
        [AggregateId] [uniqueidentifier] NOT NULL,
        [ETagValue] [nvarchar](50) NOT NULL,     
        [CreatedTime] [datetimeoffset](7) NOT NULL
     CONSTRAINT [PK_ETag] PRIMARY KEY CLUSTERED 
    (
        [AggregateId] ASC,
        [ETagValue] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
end
