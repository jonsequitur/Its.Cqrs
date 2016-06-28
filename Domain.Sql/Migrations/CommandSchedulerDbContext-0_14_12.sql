
CREATE unique  INDEX IU_AggregateId_SequenceNumber_Temp on [Scheduler].[ScheduledCommand](
[AggregateId],
[SequenceNumber]
)

ALTER TABLE [Scheduler].[ScheduledCommand] DROP CONSTRAINT [PK_Scheduler.ScheduledCommand]

ALTER TABLE [Scheduler].[ScheduledCommand] 
ADD CONSTRAINT [PK_Scheduler.ScheduledCommand] PRIMARY KEY NONCLUSTERED (
    [AggregateId] ASC,
	[SequenceNumber] ASC
)

CREATE CLUSTERED INDEX [IX_AggregateId_DueTime_ClockId_AppliedTime_FinalAttemptTime]
    ON [Scheduler].[ScheduledCommand]([AggregateId] ASC, [DueTime] ASC, [Clock_Id] ASC, [AppliedTime] ASC, [FinalAttemptTime] ASC)

DROP INDEX IU_AggregateId_SequenceNumber_Temp on [Scheduler].[ScheduledCommand]
