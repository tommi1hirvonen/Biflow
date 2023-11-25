CREATE TABLE [app].[ExecutionStepConditionParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
    [ParameterValue] SQL_VARIANT NULL,
	[ParameterValueType] VARCHAR(20) NOT NULL,
	[ExecutionParameterId] UNIQUEIDENTIFIER NULL,
    [ExecutionParameterValue] SQL_VARIANT NULL,
    CONSTRAINT [PK_ExecutionStepConditionParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionStepConditionParameter_ExecutionParameter] FOREIGN KEY ([ExecutionId], [ExecutionParameterId]) REFERENCES [app].[ExecutionParameter] ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionStepConditionParameter_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [app].[ExecutionStep] ([ExecutionId], [StepId]) ON DELETE CASCADE,
    CONSTRAINT [CK_ExecutionStepConditionParameter_ParameterValueType] CHECK (
        [ParameterValueType] = 'Boolean' OR
        [ParameterValueType] = 'DateTime' OR
        [ParameterValueType] = 'Decimal' OR
        [ParameterValueType] = 'Double' OR
        [ParameterValueType] = 'Int16' OR
        [ParameterValueType] = 'Int32' OR
        [ParameterValueType] = 'Int64' OR
        [ParameterValueType] = 'Single' OR
        [ParameterValueType] = 'String'
        )
)
