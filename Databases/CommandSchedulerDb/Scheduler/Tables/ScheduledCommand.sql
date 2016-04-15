CREATE TABLE [Scheduler].[ScheduledCommand] (
    [AggregateId]       UNIQUEIDENTIFIER   NOT NULL,
    [SequenceNumber]    BIGINT             NOT NULL,
    [AggregateType]     NVARCHAR (MAX)     NULL,
    [CreatedTime]       DATETIMEOFFSET (7) NOT NULL,
    [DueTime]           DATETIMEOFFSET (7) NULL,
    [AppliedTime]       DATETIMEOFFSET (7) NULL,
    [FinalAttemptTime]  DATETIMEOFFSET (7) NULL,
    [SerializedCommand] NVARCHAR (MAX)     NOT NULL,
    [Attempts]          INT                NOT NULL,
    [Clock_Id]          INT                NOT NULL,
    CONSTRAINT [PK_Scheduler.ScheduledCommand] PRIMARY KEY NONCLUSTERED ([AggregateId] ASC, [SequenceNumber] ASC),
    CONSTRAINT [FK_Scheduler.ScheduledCommand_Scheduler.Clock_Clock_Id] FOREIGN KEY ([Clock_Id]) REFERENCES [Scheduler].[Clock] ([Id]) ON DELETE CASCADE
);


GO
CREATE CLUSTERED INDEX [IX_AppliedTime_Clock_Id_FinalAttemptTime_DueTime]
    ON [Scheduler].[ScheduledCommand]([AppliedTime] ASC, [Clock_Id] ASC, [FinalAttemptTime] ASC, [DueTime] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Clock_Id]
    ON [Scheduler].[ScheduledCommand]([Clock_Id] ASC);

