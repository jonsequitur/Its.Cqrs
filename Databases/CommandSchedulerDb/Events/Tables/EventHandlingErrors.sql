CREATE TABLE [Events].[EventHandlingErrors] (
    [Id]              BIGINT             IDENTITY (1, 1) NOT NULL,
    [Actor]           NVARCHAR (MAX)     NULL,
    [Handler]         NVARCHAR (MAX)     NULL,
    [SequenceNumber]  BIGINT             NOT NULL,
    [AggregateId]     UNIQUEIDENTIFIER   NOT NULL,
    [StreamName]      NVARCHAR (MAX)     NULL,
    [EventTypeName]   NVARCHAR (MAX)     NULL,
    [UtcTime]         DATETIMEOFFSET (7) NOT NULL,
    [SerializedEvent] NVARCHAR (MAX)     NULL,
    [Error]           NVARCHAR (MAX)     NULL,
    [OriginalId]      BIGINT             NULL,
    CONSTRAINT [PK_Events.EventHandlingErrors] PRIMARY KEY CLUSTERED ([Id] ASC)
);

