CREATE TABLE [etlmanager].[Step] (
    [JobId]                     UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_Step_Job] FOREIGN KEY ([JobId]) REFERENCES [etlmanager].[Job] ([JobId]),
    [StepId]                    UNIQUEIDENTIFIER NOT NULL,
    [StepName]                  NVARCHAR(250)    NOT NULL,
    [StepDescription]           NVARCHAR(MAX)   NULL,
    [ExecutionPhase]            INT              NOT NULL,
    [StepType]                  VARCHAR (20)     NOT NULL,
    [SqlStatement]              NVARCHAR (MAX)   NULL,
    [ConnectionId]              UNIQUEIDENTIFIER CONSTRAINT [FK_Step_Connection] FOREIGN KEY REFERENCES etlmanager.Connection ([ConnectionId]) NULL,
    [PackageFolderName]         NVARCHAR(128)    NULL,
    [PackageProjectName]        NVARCHAR(128)    NULL,
    [PackageName]               NVARCHAR(260)    NULL,
    [ExecuteIn32BitMode]        BIT              CONSTRAINT [DF_Step_ExecuteIn32BitMode] DEFAULT (0) NOT NULL,
    [ExecuteAsLogin]            NVARCHAR(128)    NULL,
    [DataFactoryId]             UNIQUEIDENTIFIER CONSTRAINT [FK_Step_DataFactory] FOREIGN KEY REFERENCES etlmanager.DataFactory ([DataFactoryId]) NULL,
    [PipelineName]              NVARCHAR(250)    NULL,
    [JobToExecuteId]            UNIQUEIDENTIFIER NULL CONSTRAINT [FK_Step_JobToExecute] FOREIGN KEY REFERENCES [etlmanager].[Job] ([JobId]),
    [JobExecuteSynchronized]    BIT              CONSTRAINT [DF_Step_JobExecuteSynchronized] DEFAULT(0) NOT NULL,
    [ExeFileName]               NVARCHAR(1000)   NULL,
    [ExeArguments]              NVARCHAR(MAX)    NULL,
    [ExeWorkingDirectory]       NVARCHAR(1000)   NULL,
    [ExeSuccessExitCode]        INT              NULL,
    [PowerBIServiceId]          UNIQUEIDENTIFIER NULL CONSTRAINT [FK_Step_PowerBIService] FOREIGN KEY REFERENCES [etlmanager].[PowerBIService] ([PowerBIServiceId]),
    [DatasetGroupId]            NVARCHAR(36)     NULL,
    [DatasetId]                 NVARCHAR(36)     NULL,
    [FunctionAppId]             UNIQUEIDENTIFIER NULL CONSTRAINT [FK_Step_FunctionApp] FOREIGN KEY REFERENCES [etlmanager].[FunctionApp] ([FunctionAppId]),
    [FunctionName]              VARCHAR(250)     NULL,
    [FunctionInput]             NVARCHAR(MAX)    NULL,
    [FunctionIsDurable]         BIT              CONSTRAINT [DF_Step_FunctionIsDurable] DEFAULT (0) NOT NULL,
    [FunctionKey]               VARCHAR(1000)    NULL,
    [IsEnabled]                 BIT              CONSTRAINT [DF_Step_IsEnabled] DEFAULT (1) NOT NULL,
    [RetryAttempts]             INT              CONSTRAINT [DF_Step_RetryAttempts] DEFAULT (0) NOT NULL,
    [RetryIntervalMinutes]      INT              CONSTRAINT [DF_Step_RetryIntervalMinutes] DEFAULT (0) NOT NULL,
    [TimeoutMinutes]            INT              CONSTRAINT [DF_Step_TimeoutMinutes] DEFAULT (0) NOT NULL,
    [CreatedDateTime]           DATETIME2 (7)    NOT NULL,
    [CreatedBy]                 NVARCHAR(250)    NULL,
    [LastModifiedDateTime]      DATETIME2 (7)    NOT NULL,
    [LastModifiedBy]            NVARCHAR(250)    NULL,
    [Timestamp]                 ROWVERSION       NOT NULL, 
    CONSTRAINT [PK_Step] PRIMARY KEY CLUSTERED ([StepId] ASC),
    CONSTRAINT [CK_Step_StepType] CHECK (
           [StepType]='SSIS' AND [PackageFolderName] IS NOT NULL AND [PackageProjectName] IS NOT NULL AND [PackageName] IS NOT NULL AND [ConnectionId] IS NOT NULL
        OR [StepType]='SQL' AND [SqlStatement] IS NOT NULL AND [ConnectionId] IS NOT NULL
        OR [StepType]='JOB' AND [JobToExecuteId] IS NOT NULL AND [JobExecuteSynchronized] IS NOT NULL
        OR [StepType]='PIPELINE' AND [DataFactoryId] IS NOT NULL AND [PipelineName] IS NOT NULL
        OR [StepType]='EXE' AND [ExeFileName] IS NOT NULL
        OR [StepType]='DATASET' AND [PowerBIServiceId] IS NOT NULL AND [DatasetGroupId] IS NOT NULL AND [DatasetId] IS NOT NULL
        OR [StepType]='FUNCTION' AND [FunctionAppId] IS NOT NULL AND [FunctionName] IS NOT NULL),
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
        DELETE FROM etlmanager.Dependency WHERE DependantOnStepId IN (SELECT StepId FROM deleted) OR StepId IN (SELECT StepId FROM deleted)
        DELETE FROM etlmanager.Step WHERE StepId IN (SELECT StepId FROM deleted)
    END