CREATE TABLE [app].[StepParameterExpressionParameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepParameterId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[InheritFromJobParameterId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_StepParameterExpressionParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_StepParameterExpressionParameter] UNIQUE ([StepParameterId], [ParameterName]),
	CONSTRAINT [FK_StepParameterExpressionParameter_StepParameter] FOREIGN KEY ([StepParameterId])
		REFERENCES [app].[StepParameter] ([ParameterId]) ON DELETE CASCADE,
	CONSTRAINT [FK_StepParameterExpressionParameter_InheritFromJobParameter] FOREIGN KEY ([InheritFromJobParameterId])
		REFERENCES [app].[JobParameter] ([ParameterId]) ON DELETE CASCADE
)
