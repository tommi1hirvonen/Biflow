CREATE TABLE [etlmanager].[Dependency] (
    [StepId]            UNIQUEIDENTIFIER NOT NULL,
    [DependantOnStepId] UNIQUEIDENTIFIER NOT NULL,
    [StrictDependency]  BIT              NOT NULL,
    [CreatedDateTime]   DATETIMEOFFSET   NOT NULL,
    [CreatedBy]         NVARCHAR(250)    NULL,
    CONSTRAINT [PK_Dependency] PRIMARY KEY CLUSTERED ([StepId], [DependantOnStepId]),
    CONSTRAINT [CK_Dependency] CHECK ([StepId]<>[DependantOnStepId]),
    CONSTRAINT [FK_Dependency_DependantOnStepId_Step] FOREIGN KEY ([DependantOnStepId]) REFERENCES [etlmanager].[Step] ([StepId]),
    CONSTRAINT [FK_Dependency_StepId_Step] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId])
)

