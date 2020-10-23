CREATE TABLE [etlmanager].[Step] (
    [JobId]                UNIQUEIDENTIFIER NOT NULL,
    [StepId]               UNIQUEIDENTIFIER NOT NULL,
    [StepName]             NVARCHAR(250)    NOT NULL,
    [ExecutionPhase]       INT              NOT NULL,
    [StepType]             VARCHAR (4)      NOT NULL,
    [SqlStatement]         NVARCHAR (MAX)   NULL,
    [ServerName]           NVARCHAR(50)     NULL,
    [FolderName]           NVARCHAR(128)    NULL,
    [ProjectName]          NVARCHAR(128)    NULL,
    [PackageName]          NVARCHAR(260)    NULL,
    [ExecuteIn32BitMode]   BIT              NOT NULL,
    [IsEnabled]            BIT              CONSTRAINT [DF_Step_IsEnabled] DEFAULT (1) NOT NULL,
    [RetryAttempts]        INT              CONSTRAINT [DF_Step_RetryAttempts] DEFAULT (0) NOT NULL,
    [RetryIntervalMinutes] INT              CONSTRAINT [DF_Step_RetryIntervalMinutes] DEFAULT (0) NOT NULL,
    [CreatedDateTime]      DATETIME2 (7)    NOT NULL,
    [CreatedBy]            NVARCHAR(250)    NULL,
    [LastModifiedDateTime] DATETIME2 (7)    NOT NULL,
    [LastModifiedBy]       NVARCHAR(250)    NULL,
    CONSTRAINT [PK_Step] PRIMARY KEY CLUSTERED ([StepId] ASC),
    CONSTRAINT [CK_Step_StepType] CHECK ([StepType]='SSIS' AND [ServerName] IS NOT NULL AND [FolderName] IS NOT NULL AND [ProjectName] IS NOT NULL AND [PackageName] IS NOT NULL OR [StepType]='SQL' AND [SqlStatement] IS NOT NULL),
    CONSTRAINT [FK_Step_Job] FOREIGN KEY ([JobId]) REFERENCES [etlmanager].[Job] ([JobId]),
    CONSTRAINT [CK_Step_Retry] CHECK ([RetryAttempts] >= 0 AND [RetryIntervalMinutes] >= 0)
);


GO

CREATE TRIGGER [etlmanager].[Trigger_Step]
    ON [etlmanager].[Step]
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON
        -- Use instead of trigger to delete linking dependencies because of SQL Server limitation with multiple cascading paths.
        -- https://support.microsoft.com/en-us/help/321843/error-message-1785-occurs-when-you-create-a-foreign-key-constraint-tha
        DELETE FROM etlmanager.Parameter WHERE StepId IN (SELECT StepId FROM deleted)
        DELETE FROM etlmanager.Dependency WHERE DependantOnStepId IN (SELECT StepId FROM deleted) OR StepId IN (SELECT StepId FROM deleted)
        DELETE FROM etlmanager.Step WHERE StepId IN (SELECT StepId FROM deleted)
    END