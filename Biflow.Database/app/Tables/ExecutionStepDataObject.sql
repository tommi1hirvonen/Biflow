CREATE TABLE [app].[ExecutionStepDataObject]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	[ReferenceType] VARCHAR(20) NOT NULL,
	[DataAttributes] NVARCHAR(4000) NOT NULL CONSTRAINT [DF_ExecutionStepDataObject_DataAttributes] DEFAULT ('[]'), -- empty json array
	CONSTRAINT [PK_ExecutionStepDataObject] PRIMARY KEY CLUSTERED ([ExecutionId], [StepId], [ObjectId], [ReferenceType]),
	CONSTRAINT [FK_ExecutionStepDataObject_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [app].[ExecutionStep] ([ExecutionId], [StepId]),
	CONSTRAINT [FK_ExecutionStepDataObject_ExecutionDataObject] FOREIGN KEY ([ExecutionId], [ObjectId]) REFERENCES [app].[ExecutionDataObject] ([ExecutionId], [ObjectId]) ON DELETE CASCADE,
	CONSTRAINT [CK_ExecutionStepDataObject_ReferenceType] CHECK ([ReferenceType] = 'Source' OR [ReferenceType] = 'Target')
)