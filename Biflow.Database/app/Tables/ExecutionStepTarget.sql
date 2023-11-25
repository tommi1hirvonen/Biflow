CREATE TABLE [app].[ExecutionStepTarget]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_ExecutionStepTarget] PRIMARY KEY CLUSTERED ([ExecutionId], [StepId], [ObjectId]),
	CONSTRAINT [FK_ExecutionStepTarget_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [app].[ExecutionStep] ([ExecutionId], [StepId]),
	CONSTRAINT [FK_ExecutionStepTarget_ExecutionDataObject] FOREIGN KEY ([ExecutionId], [ObjectId]) REFERENCES [app].[ExecutionDataObject] ([ExecutionId], [ObjectId]) ON DELETE CASCADE
)