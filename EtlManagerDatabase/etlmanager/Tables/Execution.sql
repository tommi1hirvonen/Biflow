CREATE TABLE [etlmanager].[Execution] (
    [ExecutionId]               UNIQUEIDENTIFIER    NOT NULL,
    [JobId]                     UNIQUEIDENTIFIER    NOT NULL,
    [JobName]                   NVARCHAR(250)       NOT NULL,
    [CreatedDateTime]           DATETIME2 (7)       NOT NULL,
    [StartDateTime]             DATETIME2 (7)       NULL,
    [EndDateTime]               DATETIME2 (7)       NULL,
    [ExecutionStatus]           VARCHAR(50)         NOT NULL,
    [DependencyMode]            BIT                 NOT NULL,
    [CreatedBy]                 NVARCHAR(250)       NULL,
    [ScheduleId]                UNIQUEIDENTIFIER    NULL,
    [ExecutorProcessId]         INT                 NULL,
    CONSTRAINT [PK_Execution] PRIMARY KEY CLUSTERED ([ExecutionId] ASC)
);

