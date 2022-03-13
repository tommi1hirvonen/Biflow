CREATE TABLE [biflow].[ExecutionConcurrency]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepType] VARCHAR(20) NOT NULL,
	[MaxParallelSteps] INT NOT NULL,
	CONSTRAINT [PK_ExecutionConcurrency] PRIMARY KEY CLUSTERED ([ExecutionId], [StepType]),
	CONSTRAINT [FK_ExecutionConcurrency_Job] FOREIGN KEY ([ExecutionId]) REFERENCES [biflow].[Execution] ([ExecutionId]) ON DELETE CASCADE,
	CONSTRAINT [CK_ExecutionConcurrency_StepType] CHECK (
        [StepType]='Package'
        OR [StepType]='Sql'
        OR [StepType]='Job'
        OR [StepType]='Pipeline'
        OR [StepType]='Exe'
        OR [StepType]='Dataset'
        OR [StepType]='Function'
        OR [StepType]='AgentJob'
        OR [StepType]='Tabular'
        OR [StepType]='Email')
)
