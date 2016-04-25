CREATE TABLE [Scheduler].[ETag] (
    [Id]                BIGINT             IDENTITY (1, 1) NOT NULL,
    [Scope]             NVARCHAR (50)      NOT NULL,
    [ETagValue]         NVARCHAR (50)      NOT NULL,
    [CreatedDomainTime] DATETIMEOFFSET (7) NOT NULL,
    [CreatedRealTime]   DATETIMEOFFSET (7) NOT NULL,
    CONSTRAINT [PK_Id] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Scope_and_ETag]
    ON [Scheduler].[ETag]([Scope] ASC, [ETagValue] ASC);

