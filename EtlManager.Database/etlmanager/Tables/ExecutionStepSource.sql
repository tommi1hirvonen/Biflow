CREATE TABLE [etlmanager].[ExecutionStepSource]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_ExecutionStepSource] PRIMARY KEY CLUSTERED ([ExecutionId], [StepId], [ObjectId]),
	CONSTRAINT [FK_ExecutionStepSource_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [etlmanager].[ExecutionStep] ([ExecutionId], [StepId]),
	CONSTRAINT [FK_ExecutionStepSource_ExecutionSourceTargetObject] FOREIGN KEY ([ExecutionId], [ObjectId]) REFERENCES [etlmanager].[ExecutionSourceTargetObject] ([ExecutionId], [ObjectId])
)