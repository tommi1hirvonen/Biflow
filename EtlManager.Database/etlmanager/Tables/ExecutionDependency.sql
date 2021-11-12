CREATE TABLE [etlmanager].[ExecutionDependency] (
    [ExecutionId]       UNIQUEIDENTIFIER NOT NULL,
    [StepId]            UNIQUEIDENTIFIER NOT NULL,
    [DependantOnStepId] UNIQUEIDENTIFIER NOT NULL,
    [StrictDependency]  BIT              NOT NULL,
    CONSTRAINT [PK_ExecutionDependency] PRIMARY KEY CLUSTERED ([ExecutionId], [StepId], [DependantOnStepId]),
    CONSTRAINT [CK_ExecutionDependency] CHECK ([StepId]<>[DependantOnStepId]),
    CONSTRAINT [FK_ExecutionDependency_DependantOnStepId_Step] FOREIGN KEY ([ExecutionId], [DependantOnStepId]) REFERENCES [etlmanager].[ExecutionStep] ([ExecutionId], [StepId]),
    CONSTRAINT [FK_ExecutionDependency_StepId_Step] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [etlmanager].[ExecutionStep] ([ExecutionId], [StepId])
)

