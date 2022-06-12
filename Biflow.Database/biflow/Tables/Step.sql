CREATE TABLE [biflow].[Step] (
    [JobId]                         UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_Step_Job] FOREIGN KEY ([JobId]) REFERENCES [biflow].[Job] ([JobId]),
    [StepId]                        UNIQUEIDENTIFIER NOT NULL,
    [StepName]                      NVARCHAR(250)    NOT NULL,
    [StepDescription]               NVARCHAR(MAX)    NULL,
    [ExecutionPhase]                INT              NOT NULL,
    [StepType]                      VARCHAR (20)     NOT NULL,
    [SqlStatement]                  NVARCHAR (MAX)   NULL,
    [ResultCaptureJobParameterId]   UNIQUEIDENTIFIER NULL CONSTRAINT [FK_Step_JobParameter] FOREIGN KEY REFERENCES [biflow].[JobParameter] ([ParameterId]) ON DELETE SET NULL,
    [ConnectionId]                  UNIQUEIDENTIFIER CONSTRAINT [FK_Step_Connection] FOREIGN KEY REFERENCES biflow.Connection ([ConnectionId]) NULL,
    [PackageFolderName]             NVARCHAR(128)    NULL,
    [PackageProjectName]            NVARCHAR(128)    NULL,
    [PackageName]                   NVARCHAR(260)    NULL,
    [ExecuteIn32BitMode]            BIT              CONSTRAINT [DF_Step_ExecuteIn32BitMode] DEFAULT (0) NOT NULL,
    [ExecuteAsLogin]                NVARCHAR(128)    NULL,
    [PipelineClientId]              UNIQUEIDENTIFIER CONSTRAINT [FK_Step_PipelineClient] FOREIGN KEY REFERENCES biflow.PipelineClient ([PipelineClientId]) NULL,
    [PipelineName]                  NVARCHAR(250)    NULL,
    [JobToExecuteId]                UNIQUEIDENTIFIER NULL CONSTRAINT [FK_Step_JobToExecute] FOREIGN KEY REFERENCES [biflow].[Job] ([JobId]),
    [JobExecuteSynchronized]        BIT              CONSTRAINT [DF_Step_JobExecuteSynchronized] DEFAULT(0) NOT NULL,
    [ExeFileName]                   NVARCHAR(1000)   NULL,
    [ExeArguments]                  NVARCHAR(MAX)    NULL,
    [ExeWorkingDirectory]           NVARCHAR(1000)   NULL,
    [ExeSuccessExitCode]            INT              NULL,
    [AppRegistrationId]             UNIQUEIDENTIFIER NULL CONSTRAINT [FK_Step_AppRegistration] FOREIGN KEY REFERENCES [biflow].[AppRegistration] ([AppRegistrationId]),
    [DatasetGroupId]                NVARCHAR(36)     NULL,
    [DatasetId]                     NVARCHAR(36)     NULL,
    [FunctionAppId]                 UNIQUEIDENTIFIER NULL CONSTRAINT [FK_Step_FunctionApp] FOREIGN KEY REFERENCES [biflow].[FunctionApp] ([FunctionAppId]),
    [FunctionUrl]                   VARCHAR(1000)    NULL,
    [FunctionInput]                 NVARCHAR(MAX)    NULL,
    [FunctionIsDurable]             BIT              CONSTRAINT [DF_Step_FunctionIsDurable] DEFAULT (0) NOT NULL,
    [FunctionKey]                   VARCHAR(1000)    NULL,
    [AgentJobName]                  NVARCHAR(128)    NULL,
    [TabularModelName]              NVARCHAR(128)    NULL,
    [TabularTableName]              NVARCHAR(128)    NULL,
    [TabularPartitionName]          NVARCHAR(128)    NULL,
    [EmailRecipients]               NVARCHAR(MAX)    NULL,
    [EmailSubject]                  NVARCHAR(MAX)    NULL,
    [EmailBody]                     NVARCHAR(MAX)    NULL,
    [IsEnabled]                     BIT              CONSTRAINT [DF_Step_IsEnabled] DEFAULT (1) NOT NULL,
    [RetryAttempts]                 INT              CONSTRAINT [DF_Step_RetryAttempts] DEFAULT (0) NOT NULL,
    [RetryIntervalMinutes]          INT              CONSTRAINT [DF_Step_RetryIntervalMinutes] DEFAULT (0) NOT NULL,
    [TimeoutMinutes]                INT              CONSTRAINT [DF_Step_TimeoutMinutes] DEFAULT (0) NOT NULL,
    [ExecutionConditionExpression]  NVARCHAR(MAX)    NULL,
    [CreatedDateTime]               DATETIMEOFFSET   NOT NULL,
    [CreatedBy]                     NVARCHAR(250)    NULL,
    [LastModifiedDateTime]          DATETIMEOFFSET   NOT NULL,
    [LastModifiedBy]                NVARCHAR(250)    NULL,
    [Timestamp]                     ROWVERSION       NOT NULL, 
    CONSTRAINT [PK_Step] PRIMARY KEY CLUSTERED ([StepId] ASC),
    INDEX [NCI_Step_Job] NONCLUSTERED ([JobId]),
    CONSTRAINT [CK_Step_StepType] CHECK (
           [StepType]='Package' AND [PackageFolderName] IS NOT NULL AND [PackageProjectName] IS NOT NULL AND [PackageName] IS NOT NULL AND [ConnectionId] IS NOT NULL
        OR [StepType]='Sql' AND [SqlStatement] IS NOT NULL AND [ConnectionId] IS NOT NULL
        OR [StepType]='Job' AND [JobToExecuteId] IS NOT NULL AND [JobExecuteSynchronized] IS NOT NULL
        OR [StepType]='Pipeline' AND [PipelineClientId] IS NOT NULL AND [PipelineName] IS NOT NULL
        OR [StepType]='Exe' AND [ExeFileName] IS NOT NULL
        OR [StepType]='Dataset' AND [AppRegistrationId] IS NOT NULL AND [DatasetGroupId] IS NOT NULL AND [DatasetId] IS NOT NULL
        OR [StepType]='Function' AND [FunctionAppId] IS NOT NULL AND [FunctionUrl] IS NOT NULL
        OR [StepType]='AgentJob' AND [AgentJobName] IS NOT NULL AND [ConnectionId] IS NOT NULL
        OR [StepType]='Tabular' AND [TabularModelName] IS NOT NULL AND NOT ([TabularTableName] IS NULL AND [TabularPartitionName] IS NOT NULL)
        OR [StepType]='Email' AND [EmailRecipients] IS NOT NULL AND [EmailSubject] IS NOT NULL AND [EmailBody] IS NOT NULL),
    CONSTRAINT [CK_Step_Retry] CHECK ([RetryAttempts] >= 0 AND [RetryIntervalMinutes] >= 0)
);


GO

CREATE TRIGGER [biflow].[Trigger_Step]
    ON [biflow].[Step]
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON
        -- Use instead of trigger to delete linking dependencies because of SQL Server limitation with multiple cascading paths.
        -- https://support.microsoft.com/en-us/help/321843/error-message-1785-occurs-when-you-create-a-foreign-key-constraint-tha
        DELETE FROM biflow.Dependency WHERE DependantOnStepId IN (SELECT StepId FROM deleted) OR StepId IN (SELECT StepId FROM deleted)
        DELETE FROM biflow.Step WHERE StepId IN (SELECT StepId FROM deleted)
    END