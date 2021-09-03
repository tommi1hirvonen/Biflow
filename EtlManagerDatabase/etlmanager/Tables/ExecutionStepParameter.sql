CREATE TABLE [etlmanager].[ExecutionStepParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
    [ParameterLevel] VARCHAR(20) NOT NULL,
	[ParameterType] VARCHAR(20) NOT NULL,
	[ExecutionParameterId] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_ExecutionStepParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionStepParameter_ExecutionParameter] FOREIGN KEY ([ExecutionId], [ExecutionParameterId]) REFERENCES [etlmanager].[ExecutionParameter] ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionStepParameter_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [etlmanager].[ExecutionStep] ([ExecutionId], [StepId]),
    CONSTRAINT [CK_ExecutionStepParameter_Parameterlevel] CHECK ([ParameterLevel] = 'Package' OR [ParameterLevel] = 'Project' OR [ParameterLevel] = 'None'),
    CONSTRAINT [CK_ExecutionStepParameter_ParameterType] CHECK (
        [ParameterType] = 'Boolean' OR
        [ParameterType] = 'Byte' OR
        [ParameterType] = 'DateTime' OR
        [ParameterType] = 'Decimal' OR
        [ParameterType] = 'Double' OR
        [ParameterType] = 'Int16' OR
        [ParameterType] = 'Int32' OR
        [ParameterType] = 'Int64' OR
        [ParameterType] = 'SByte' OR
        [ParameterType] = 'Single' OR
        [ParameterType] = 'String' OR
        [ParameterType] = 'UInt32' OR
        [ParameterType] = 'UInt64'
        )
)
