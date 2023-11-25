CREATE TABLE [app].[ExecutionStepParameterExpressionParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepParameterId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[InheritFromExecutionParameterId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_ExecutionStepParameterExpressionParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [ParameterId]),
	CONSTRAINT [UQ_ExecutionStepParameterExpressionParameter] UNIQUE ([ExecutionId], [StepParameterId], [ParameterName]),
	CONSTRAINT [FK_ExecutionStepParameterExpressionParameter_ExecutionStepParameter] FOREIGN KEY ([ExecutionId], [StepParameterId])
		REFERENCES [app].[ExecutionStepParameter] ([ExecutionId], [ParameterId]) ON DELETE CASCADE,
	CONSTRAINT [FK_ExecutionStepParameterExpressionParameter_InheritFromExecutionParameter] FOREIGN KEY ([ExecutionId], [InheritFromExecutionParameterId])
		REFERENCES [app].[ExecutionParameter] ([ExecutionId], [ParameterId]) ON DELETE CASCADE
)
