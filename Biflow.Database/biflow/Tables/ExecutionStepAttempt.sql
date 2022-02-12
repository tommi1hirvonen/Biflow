CREATE TABLE [biflow].[ExecutionStepAttempt] (
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
    [ErrorMessage]              NVARCHAR(MAX)       NULL,
    [InfoMessage]               NVARCHAR(MAX)       NULL,
    [StoppedBy]                 NVARCHAR(250)       NULL,
    CONSTRAINT [PK_ExecutionStepAttempt] PRIMARY KEY CLUSTERED ([ExecutionId] ASC, [StepId] ASC, [RetryAttemptIndex] ASC),
    CONSTRAINT [FK_ExecutionStepAttempt_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [biflow].ExecutionStep ([ExecutionId], [StepId]) ON DELETE CASCADE,
    CONSTRAINT [CK_ExecutionStepAttempt_StepType] CHECK (
        [StepType]='Package'
        OR [StepType]='Sql'
        OR [StepType]='Job'
        OR [StepType]='Pipeline'
        OR [StepType]='Exe'
        OR [StepType]='Dataset'
        OR [StepType]='Function'
        OR [StepType]='AgentJob'
        OR [StepType]='Tabular'),
    CONSTRAINT [CK_ExecutionStepAttempt_ExecutionStatus] CHECK (
        [ExecutionStatus] = 'NotStarted'
        OR [ExecutionStatus] = 'Running'
        OR [ExecutionStatus] = 'Succeeded'
        OR [ExecutionStatus] = 'Failed'
        OR [ExecutionStatus] = 'Stopped'
        OR [ExecutionStatus] = 'Skipped'
        OR [ExecutionStatus] = 'AwaitRetry'
        OR [ExecutionStatus] = 'Duplicate')
);