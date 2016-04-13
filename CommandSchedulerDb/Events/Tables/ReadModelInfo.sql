CREATE TABLE [Events].[ReadModelInfo] (
    [Name]                    NVARCHAR (256)     NOT NULL,
    [LastUpdated]             DATETIMEOFFSET (7) NULL,
    [CurrentAsOfEventId]      BIGINT             NOT NULL,
    [FailedOnEventId]         BIGINT             NULL,
    [Error]                   NVARCHAR (MAX)     NULL,
    [LatencyInMilliseconds]   FLOAT (53)         NOT NULL,
    [InitialCatchupStartTime] DATETIMEOFFSET (7) NULL,
    [InitialCatchupEvents]    BIGINT             DEFAULT ((0)) NOT NULL,
    [InitialCatchupEndTime]   DATETIMEOFFSET (7) NULL,
    [BatchRemainingEvents]    BIGINT             DEFAULT ((0)) NOT NULL,
    [BatchStartTime]          DATETIMEOFFSET (7) NULL,
    [BatchTotalEvents]        BIGINT             DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Events.ReadModelInfo] PRIMARY KEY CLUSTERED ([Name] ASC)
);

