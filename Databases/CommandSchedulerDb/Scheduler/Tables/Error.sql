CREATE TABLE [Scheduler].[Error] (
    [Id]                              BIGINT           IDENTITY (1, 1) NOT NULL,
    [Error]                           NVARCHAR (MAX)   NOT NULL,
    [ScheduledCommand_AggregateId]    UNIQUEIDENTIFIER NOT NULL,
    [ScheduledCommand_SequenceNumber] BIGINT           NOT NULL,
    CONSTRAINT [PK_Scheduler.Error] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_ScheduledCommand_AggregateId_ScheduledCommand_SequenceNumber]
    ON [Scheduler].[Error]([ScheduledCommand_AggregateId] ASC, [ScheduledCommand_SequenceNumber] ASC);

