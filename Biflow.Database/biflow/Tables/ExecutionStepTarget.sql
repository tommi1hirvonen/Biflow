CREATE TABLE [biflow].[ExecutionStepTarget]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_ExecutionStepTarget] PRIMARY KEY CLUSTERED ([ExecutionId], [StepId], [ObjectId]),
	CONSTRAINT [FK_ExecutionStepTarget_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [biflow].[ExecutionStep] ([ExecutionId], [StepId]),
	CONSTRAINT [FK_ExecutionStepTarget_ExecutionDataObject] FOREIGN KEY ([ExecutionId], [ObjectId]) REFERENCES [biflow].[ExecutionDataObject] ([ExecutionId], [ObjectId]) ON DELETE CASCADE
)