-- make sure the unique index is in place
IF NOT EXISTS(SELECT * FROM sys.indexes 
              WHERE name='IX_Scope_and_ETag' 
              AND object_id = OBJECT_ID('Scheduler.ETag'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Scope_and_ETag] ON [Scheduler].[ETag]
    (
        [Scope] ASC,
        [ETagValue] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
END


IF EXISTS (SELECT * FROM sys.foreign_keys
           WHERE object_id = (SELECT object_id 
           FROM sys.objects 
           WHERE name = 'FK_Scheduler.Error_Scheduler.ScheduledCommand_ScheduledCommand_AggregateId_ScheduledCommand_SequenceNumber')) 
BEGIN
    ALTER TABLE [Scheduler].[Error] 
    DROP CONSTRAINT [FK_Scheduler.Error_Scheduler.ScheduledCommand_ScheduledCommand_AggregateId_ScheduledCommand_SequenceNumber]
END


IF NOT EXISTS(SELECT * FROM sys.indexes 
              WHERE name='IX_Temp' 
              AND object_id = OBJECT_ID('Scheduler.ScheduledCommand'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Temp] on [Scheduler].[ScheduledCommand]
    (
        [AggregateId] ASC,
        [SequenceNumber] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

    ALTER TABLE [Scheduler].[ScheduledCommand] DROP CONSTRAINT [PK_Scheduler.ScheduledCommand]

    ALTER TABLE [Scheduler].[ScheduledCommand] ADD CONSTRAINT [PK_Scheduler.ScheduledCommand] PRIMARY KEY NONCLUSTERED 
    (
        [AggregateId] ASC,
        [SequenceNumber] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

    DROP INDEX [IX_Temp] ON [Scheduler].[ScheduledCommand]
    
    CREATE CLUSTERED INDEX [IX_AppliedTime_Clock_Id_FinalAttemptTime_DueTime] ON 
    [Scheduler].[ScheduledCommand] 
    (
        [AppliedTime], 
        [Clock_Id], 
        [FinalAttemptTime], 
        [DueTime]
     ) 
    WITH (ONLINE = OFF)
END
