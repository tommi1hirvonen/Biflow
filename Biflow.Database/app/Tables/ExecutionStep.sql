CREATE TABLE [app].[ExecutionStep] (
    [ExecutionId]                       UNIQUEIDENTIFIER    NOT NULL CONSTRAINT [FK_ExecutionStep_Execution] FOREIGN KEY REFERENCES [app].[Execution] ([ExecutionId]),
    [StepId]                            UNIQUEIDENTIFIER    NOT NULL,
    [StepName]                          NVARCHAR(250)       NOT NULL,
    [RetryAttempts]                     INT                 NOT NULL,
    [RetryIntervalMinutes]              FLOAT               NOT NULL,
    [TimeoutMinutes]                    FLOAT               NOT NULL CONSTRAINT [DF_ExecutionStep_TimeoutMinutes] DEFAULT (0),
    [ExecutionConditionExpression]      NVARCHAR(MAX)       NULL,
    [ExecutionPhase]                    INT                 NOT NULL,
    [StepType]                          VARCHAR (20)        NOT NULL,
    [SqlStatement]                      NVARCHAR (MAX)      NULL,
    [ResultCaptureJobParameterId]       UNIQUEIDENTIFIER    NULL,
    [ResultCaptureJobParameterValue]    SQL_VARIANT         NULL,
    [ConnectionId]                      UNIQUEIDENTIFIER    NULL,
    [PackageFolderName]                 NVARCHAR(128)       NULL,
    [PackageProjectName]                NVARCHAR(128)       NULL,
    [PackageName]                       NVARCHAR(260)       NULL,
    [ExecuteIn32BitMode]                BIT                 NOT NULL CONSTRAINT [DF_ExecutionStep_ExecuteIn32BitMode] DEFAULT(0),
    [ExecuteAsLogin]                    NVARCHAR(128)       NULL,
    [PipelineClientId]                  UNIQUEIDENTIFIER    NULL,
    [PipelineName]                      NVARCHAR(250)       NULL,
    [JobToExecuteId]                    UNIQUEIDENTIFIER    NULL,
    [JobExecuteSynchronized]            BIT                 NULL,
    [ExeFileName]                       NVARCHAR(1000)      NULL,
    [ExeArguments]                      NVARCHAR(MAX)       NULL,
    [ExeWorkingDirectory]               NVARCHAR(1000)      NULL,
    [ExeSuccessExitCode]                INT                 NULL,
    [AppRegistrationId]                 UNIQUEIDENTIFIER    NULL,
    [DatasetGroupId]                    NVARCHAR(36)        NULL,
    [DatasetId]                         NVARCHAR(36)        NULL,
    [FunctionAppId]                     UNIQUEIDENTIFIER    NULL,
    [FunctionUrl]                       VARCHAR(1000)       NULL,
    [FunctionInput]                     NVARCHAR(MAX)       NULL,
    [FunctionIsDurable]                 BIT                 NULL,
    [AgentJobName]                      NVARCHAR(128)       NULL,
    [TabularModelName]                  NVARCHAR(128)       NULL,
    [TabularTableName]                  NVARCHAR(128)       NULL,
    [TabularPartitionName]              NVARCHAR(128)       NULL,
    [EmailRecipients]                   NVARCHAR(MAX)       NULL,
    [EmailSubject]                      NVARCHAR(MAX)       NULL,
    [EmailBody]                         NVARCHAR(MAX)       NULL,
    [DuplicateExecutionBehaviour]       VARCHAR(20)         CONSTRAINT [DF_ExecutionStep_DuplicateExecutionBehaviour] DEFAULT ('Wait') NOT NULL,
    [TagFilters]                        VARCHAR(MAX)        NULL, -- JSON array of tag ids
    [AppId]                             NVARCHAR(36)        NULL,
    [QlikCloudClientId]                 UNIQUEIDENTIFIER    NULL,
    CONSTRAINT [PK_ExecutionStep] PRIMARY KEY CLUSTERED ([ExecutionId] ASC, [StepId] ASC),
    CONSTRAINT [FK_ExecutionStep_ExecutionParameter] FOREIGN KEY ([ExecutionId], [ResultCaptureJobParameterId]) REFERENCES [app].[ExecutionParameter] ([ExecutionId], [ParameterId]),
    CONSTRAINT [CK_ExecutionStep_StepType] CHECK (
        [StepType]='Package'
        OR [StepType]='Sql'
        OR [StepType]='Job'
        OR [StepType]='Pipeline'
        OR [StepType]='Exe'
        OR [StepType]='Dataset'
        OR [StepType]='Function'
        OR [StepType]='AgentJob'
        OR [StepType]='Tabular'
        OR [StepType]='Email'
        OR [StepType]='Qlik'),
    CONSTRAINT [CK_ExecutionStep_DuplicateExecutionBehaviour] CHECK (
        [DuplicateExecutionBehaviour] = 'Allow'
        OR [DuplicateExecutionBehaviour] = 'Wait'
        OR [DuplicateExecutionBehaviour] = 'Fail')
);

GO

CREATE TRIGGER [app].[Trigger_ExecutionStep]
    ON [app].[ExecutionStep]
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON
        -- Use instead of trigger to delete linking dependencies because of SQL Server limitation with multiple cascading paths.
        -- https://support.microsoft.com/en-us/help/321843/error-message-1785-occurs-when-you-create-a-foreign-key-constraint-tha
        
        DELETE FROM app.ExecutionDependency
            WHERE EXISTS (
                SELECT *
                FROM deleted
                WHERE ExecutionDependency.ExecutionId = deleted.ExecutionId AND ExecutionDependency.StepId = deleted.StepId
            ) OR EXISTS (
                SELECT *
                FROM deleted
                WHERE ExecutionDependency.ExecutionId = deleted.ExecutionId AND ExecutionDependency.DependantOnStepId = deleted.StepId
            )

        DELETE FROM app.ExecutionStepSource
        WHERE EXISTS (
            SELECT *
            FROM deleted
            WHERE ExecutionStepSource.ExecutionId = deleted.ExecutionId AND ExecutionStepSource.StepId = deleted.StepId
        )

        DELETE FROM app.ExecutionStepTarget
        WHERE EXISTS (
            SELECT *
            FROM deleted
            WHERE ExecutionStepTarget.ExecutionId = deleted.ExecutionId AND ExecutionStepTarget.StepId = deleted.StepId
        )
        
        DELETE FROM app.ExecutionStep
            WHERE EXISTS (
                SELECT *
                FROM deleted
                WHERE ExecutionStep.ExecutionId = deleted.ExecutionId AND ExecutionStep.StepId = deleted.StepId
            )
            
    END