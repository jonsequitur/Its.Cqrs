CREATE TABLE [Scheduler].[Clock] (
    [Id]        INT                IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (128)     NOT NULL,
    [StartTime] DATETIMEOFFSET (7) NOT NULL,
    [UtcNow]    DATETIMEOFFSET (7) NOT NULL,
    CONSTRAINT [PK_Scheduler.Clock] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Name]
    ON [Scheduler].[Clock]([Name] ASC);

