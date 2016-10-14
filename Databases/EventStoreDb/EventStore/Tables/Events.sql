CREATE TABLE [EventStore].[Events] (
    [AggregateId]    UNIQUEIDENTIFIER NOT NULL,
    [SequenceNumber] BIGINT           NOT NULL,
    [Id]             BIGINT           IDENTITY (1, 1) NOT NULL,
    [StreamName]     NVARCHAR (50)   NOT NULL,
    [Type]           NVARCHAR (100)   NOT NULL,
    [UtcTime]        DATETIME         NOT NULL,
    [Actor]          NVARCHAR (255)   NULL,
    [Body]           NVARCHAR (MAX)   NULL,
    [ETag]           NVARCHAR (100)   NULL,
    CONSTRAINT [PK_EventStore.Events] PRIMARY KEY CLUSTERED ([AggregateId] ASC, [SequenceNumber] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_ETag]
    ON [EventStore].[Events]([ETag] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_Id   
    ON EventStore.[Events] (Id); 
GO
