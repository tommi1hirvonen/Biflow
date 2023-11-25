CREATE TABLE [app].[ExecutionStepAttempt] (
    [ExecutionId]               UNIQUEIDENTIFIER    NOT NULL,
    [StepId]                    UNIQUEIDENTIFIER    NOT NULL,
    [RetryAttemptIndex]         INT                 NOT NULL,
    [StartDateTime]             DATETIMEOFFSET      NULL,
    [EndDateTime]               DATETIMEOFFSET      NULL,
    [ExecutionStatus]           VARCHAR(50)         NOT NULL,
    [StepType]                  VARCHAR (20)        NOT NULL,
    [PackageOperationId]        BIGINT              NULL,
    [PipelineRunId]             NVARCHAR(250)       NULL,
    [FunctionInstanceId]        VARCHAR(250)        NULL,
    [ChildJobExecutionId]       UNIQUEIDENTIFIER    NULL,
    [ExeProcessId]              INT                 NULL,
    [ErrorMessages]             NVARCHAR(MAX)       NOT NULL,
    [WarningMessages]           NVARCHAR(MAX)       NOT NULL,
    [InfoMessages]              NVARCHAR(MAX)       NOT NULL,
    [StoppedBy]                 NVARCHAR(250)       NULL,
    [ReloadId]                  NVARCHAR(50)        NULL,
    CONSTRAINT [PK_ExecutionStepAttempt] PRIMARY KEY CLUSTERED ([ExecutionId] ASC, [StepId] ASC, [RetryAttemptIndex] ASC),
    CONSTRAINT [FK_ExecutionStepAttempt_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [app].ExecutionStep ([ExecutionId], [StepId]) ON DELETE CASCADE,
    CONSTRAINT [CK_ExecutionStepAttempt_StepType] CHECK (
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
    CONSTRAINT [CK_ExecutionStepAttempt_ExecutionStatus] CHECK (
        [ExecutionStatus] = 'NotStarted'
        OR [ExecutionStatus] = 'Queued'
        OR [ExecutionStatus] = 'Running'
        OR [ExecutionStatus] = 'Succeeded'
        OR [ExecutionStatus] = 'Warning'
        OR [ExecutionStatus] = 'Failed'
        OR [ExecutionStatus] = 'Retry'
        OR [ExecutionStatus] = 'Stopped'
        OR [ExecutionStatus] = 'Skipped'
        OR [ExecutionStatus] = 'DependenciesFailed'
        OR [ExecutionStatus] = 'AwaitingRetry'
        OR [ExecutionStatus] = 'Duplicate')
);