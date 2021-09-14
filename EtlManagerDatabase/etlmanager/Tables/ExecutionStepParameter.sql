CREATE TABLE [etlmanager].[ExecutionStepParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
    [ParameterType] VARCHAR(20) NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
    [ParameterLevel] VARCHAR(20) NULL,
    [ParameterValue] SQL_VARIANT NOT NULL,
	[ParameterValueType] VARCHAR(20) NOT NULL,
	[ExecutionParameterId] UNIQUEIDENTIFIER NULL,
    [ExecutionParameterValue] SQL_VARIANT NULL,
    CONSTRAINT [PK_ExecutionStepParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionStepParameter_ExecutionParameter] FOREIGN KEY ([ExecutionId], [ExecutionParameterId]) REFERENCES [etlmanager].[ExecutionParameter] ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionStepParameter_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [etlmanager].[ExecutionStep] ([ExecutionId], [StepId]),
    CONSTRAINT [CK_ExecutionStepParameter_ParameterType] CHECK ([ParameterType] = 'Package' OR [ParameterType] = 'Base'),
    CONSTRAINT [CK_ExecutionStepParameter_ParameterLevel] CHECK ([ParameterLevel] = 'Package' OR [ParameterLevel] = 'Project' OR [ParameterLevel] IS NULL),
    CONSTRAINT [CK_ExecutionStepParameter_ParameterValueType] CHECK (
        [ParameterValueType] = 'Boolean' OR
        [ParameterValueType] = 'Byte' OR
        [ParameterValueType] = 'DateTime' OR
        [ParameterValueType] = 'Decimal' OR
        [ParameterValueType] = 'Double' OR
        [ParameterValueType] = 'Int16' OR
        [ParameterValueType] = 'Int32' OR
        [ParameterValueType] = 'Int64' OR
        [ParameterValueType] = 'SByte' OR
        [ParameterValueType] = 'Single' OR
        [ParameterValueType] = 'String'
        )
)
