CREATE TABLE [biflow].[ExecutionStepParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
    [ParameterType] VARCHAR(20) NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
    [ParameterLevel] VARCHAR(20) NULL,
    [ParameterValue] SQL_VARIANT NULL,
	[ParameterValueType] VARCHAR(20) NOT NULL,
	[InheritFromExecutionParameterId] UNIQUEIDENTIFIER NULL,
    [ExecutionParameterValue] SQL_VARIANT NULL,
    [AssignToJobParameterId] UNIQUEIDENTIFIER NULL,
    [UseExpression] BIT NOT NULL CONSTRAINT [DF_ExecutionStepParameter_UseExpression] DEFAULT (0),
    [Expression] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_ExecutionStepParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionStepParameter_InheritFromExecutionParameter] FOREIGN KEY ([ExecutionId], [InheritFromExecutionParameterId]) REFERENCES [biflow].[ExecutionParameter] ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionStepParameter_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [biflow].[ExecutionStep] ([ExecutionId], [StepId]) ON DELETE CASCADE,
    CONSTRAINT [CK_ExecutionStepParameter_ParameterType] CHECK (
        [ParameterType] = 'Package' OR
        [ParameterType] = 'Job' OR
        [ParameterType] = 'Sql' OR
        [ParameterType] = 'Pipeline' OR
        [ParameterType] = 'Exe' OR
        [ParameterType] = 'Function' OR
        [ParameterType] = 'Email'
        ),
    CONSTRAINT [CK_ExecutionStepParameter_ParameterLevel] CHECK ([ParameterLevel] = 'Package' OR [ParameterLevel] = 'Project' OR [ParameterLevel] IS NULL),
    CONSTRAINT [CK_ExecutionStepParameter_JobStep] CHECK ([ParameterType] <> 'Job' OR [ParameterType] = 'Job' AND [AssignToJobParameterId] IS NOT NULL),
    CONSTRAINT [CK_ExecutionStepParameter_ParameterValueType] CHECK (
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
