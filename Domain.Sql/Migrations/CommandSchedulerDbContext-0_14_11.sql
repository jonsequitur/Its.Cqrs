-- make sure the unique index is in place
IF NOT EXISTS(SELECT * FROM sys.indexes 
              WHERE name='IX_Scope_and_ETag' 
              AND object_id = OBJECT_ID('Scheduler.ETag'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Scope_and_ETag] ON [Scheduler].[ETag]
    (
        [Scope] ASC,
        [ETagValue] ASC
    )
END

IF EXISTS (SELECT * FROM sys.foreign_keys
           WHERE object_id = (SELECT object_id 
           FROM sys.objects 
           WHERE name = 'FK_Scheduler.Error_Scheduler.ScheduledCommand_ScheduledCommand_AggregateId_ScheduledCommand_SequenceNumber')) 
BEGIN
    ALTER TABLE [Scheduler].[Error] 
    DROP CONSTRAINT [FK_Scheduler.Error_Scheduler.ScheduledCommand_ScheduledCommand_AggregateId_ScheduledCommand_SequenceNumber]
END

