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

