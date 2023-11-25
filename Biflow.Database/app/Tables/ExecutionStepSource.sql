CREATE TABLE [app].[ExecutionStepSource]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_ExecutionStepSource] PRIMARY KEY CLUSTERED ([ExecutionId], [StepId], [ObjectId]),
	CONSTRAINT [FK_ExecutionStepSource_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [app].[ExecutionStep] ([ExecutionId], [StepId]),
	CONSTRAINT [FK_ExecutionStepSource_ExecutionDataObject] FOREIGN KEY ([ExecutionId], [ObjectId]) REFERENCES [app].[ExecutionDataObject] ([ExecutionId], [ObjectId]) ON DELETE CASCADE
)