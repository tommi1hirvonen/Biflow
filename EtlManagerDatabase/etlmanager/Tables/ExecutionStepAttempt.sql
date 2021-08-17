CREATE TABLE [etlmanager].[ExecutionStepAttempt] (
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
    CONSTRAINT [FK_ExecutionStepAttempt_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [etlmanager].ExecutionStep ([ExecutionId], [StepId]),
    CONSTRAINT [CK_ExecutionStepAttempt_StepType] CHECK ([StepType]='SSIS' OR [StepType]='SQL' OR [StepType]='JOB' OR [StepType]='PIPELINE' OR [StepType]='EXE' OR [StepType]='DATASET' OR [StepType]='FUNCTION')
);