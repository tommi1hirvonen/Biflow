CREATE TABLE [biflow].[ExecutionJobStepTagFilter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[TagId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_ExecutionJobStepTag] PRIMARY KEY CLUSTERED ([ExecutionId], [StepId], [TagId]),
	CONSTRAINT [FK_ExecutionJobStepTagFilter_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [biflow].[ExecutionStep] ([ExecutionId], [StepId]) ON DELETE CASCADE
)
