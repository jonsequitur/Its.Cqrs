CREATE TABLE [Scheduler].[ClockMapping] (
    [Id]       BIGINT         IDENTITY (1, 1) NOT NULL,
    [Value]    NVARCHAR (128) NOT NULL,
    [Clock_Id] INT            NOT NULL,
    CONSTRAINT [PK_Scheduler.ClockMapping] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Scheduler.ClockMapping_Scheduler.Clock_Clock_Id] FOREIGN KEY ([Clock_Id]) REFERENCES [Scheduler].[Clock] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Clock_Id]
    ON [Scheduler].[ClockMapping]([Clock_Id] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Value]
    ON [Scheduler].[ClockMapping]([Value] ASC);

