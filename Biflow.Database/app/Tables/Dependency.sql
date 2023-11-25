CREATE TABLE [app].[Dependency] (
    [StepId]            UNIQUEIDENTIFIER NOT NULL,
    [DependantOnStepId] UNIQUEIDENTIFIER NOT NULL,
    [DependencyType]    VARCHAR(20)      NOT NULL,
    [CreatedDateTime]   DATETIMEOFFSET   NOT NULL,
    [CreatedBy]         NVARCHAR(250)    NULL,
    CONSTRAINT [PK_Dependency] PRIMARY KEY CLUSTERED ([StepId], [DependantOnStepId]),
    CONSTRAINT [CK_Dependency] CHECK ([StepId]<>[DependantOnStepId]),
    CONSTRAINT [FK_Dependency_DependantOnStepId_Step] FOREIGN KEY ([DependantOnStepId]) REFERENCES [app].[Step] ([StepId]),
    CONSTRAINT [FK_Dependency_StepId_Step] FOREIGN KEY ([StepId]) REFERENCES [app].[Step] ([StepId]),
    CONSTRAINT [CK_Dependency_DependencyType] CHECK (
        [DependencyType] = 'OnSucceeded' OR
        [DependencyType] = 'OnCompleted' OR
        [DependencyType] = 'OnFailed'
    )
)

