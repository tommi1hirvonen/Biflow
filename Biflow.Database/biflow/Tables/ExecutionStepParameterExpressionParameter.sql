CREATE TABLE [biflow].[ExecutionStepParameterExpressionParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepParameterId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[InheritFromExecutionParameterId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_ExecutionStepParameterExpressionParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [StepParameterId], [ParameterName]),
	CONSTRAINT [FK_ExecutionStepParameterExpressionParameter_ExecutionStepParameter] FOREIGN KEY ([ExecutionId], [StepParameterId])
		REFERENCES [biflow].[ExecutionStepParameter] ([ExecutionId], [ParameterId]) ON DELETE CASCADE,
	CONSTRAINT [FK_ExecutionStepParameterExpressionParameter_InheritFromExecutionParameter] FOREIGN KEY ([ExecutionId], [InheritFromExecutionParameterId])
		REFERENCES [biflow].[ExecutionParameter] ([ExecutionId], [ParameterId]) ON DELETE CASCADE
)
