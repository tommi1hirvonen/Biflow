CREATE TABLE [app].[JobConcurrency]
(
	[JobId] UNIQUEIDENTIFIER NOT NULL,
	[StepType] VARCHAR(20) NOT NULL,
	[MaxParallelSteps] INT NOT NULL,
	CONSTRAINT [PK_JobConcurrency] PRIMARY KEY CLUSTERED ([JobId], [StepType]),
	CONSTRAINT [FK_JobConcurrency_Job] FOREIGN KEY ([JobId]) REFERENCES [app].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [CK_JobConcurrency_StepType] CHECK (
        [StepType]='Package'
        OR [StepType]='Sql'
        OR [StepType]='Job'
        OR [StepType]='Pipeline'
        OR [StepType]='Exe'
        OR [StepType]='Dataset'
        OR [StepType]='Function'
        OR [StepType]='AgentJob'
        OR [StepType]='Tabular')
)
