CREATE TABLE [app].[ExecutionDependency] (
    [ExecutionId]       UNIQUEIDENTIFIER NOT NULL,
    [StepId]            UNIQUEIDENTIFIER NOT NULL,
    [DependantOnStepId] UNIQUEIDENTIFIER NOT NULL,
    [DependencyType]    VARCHAR(20)      NOT NULL,
    CONSTRAINT [PK_ExecutionDependency] PRIMARY KEY CLUSTERED ([ExecutionId], [StepId], [DependantOnStepId]),
    CONSTRAINT [CK_ExecutionDependency] CHECK ([StepId]<>[DependantOnStepId]),
    CONSTRAINT [FK_ExecutionDependency_StepId_Step] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [app].[ExecutionStep] ([ExecutionId], [StepId]),
    CONSTRAINT [CK_ExecutionDependency_DependencyType] CHECK (
        [DependencyType] = 'OnSucceeded' OR
        [DependencyType] = 'OnCompleted' OR
        [DependencyType] = 'OnFailed'
    )
)

