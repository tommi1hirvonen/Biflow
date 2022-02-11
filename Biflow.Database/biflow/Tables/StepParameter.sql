CREATE TABLE [biflow].[StepParameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterType] VARCHAR(20) NOT NULL,
    [ParameterLevel] VARCHAR(20) NULL,
    [ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValueType] VARCHAR(20) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
	[JobParameterId] UNIQUEIDENTIFIER NULL CONSTRAINT [FK_StepParameter_JobParameter] FOREIGN KEY REFERENCES [biflow].[JobParameter] ([ParameterId]) ON DELETE SET NULL,
    CONSTRAINT [PK_StepParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_StepParameter] UNIQUE ([StepId], [ParameterLevel], [ParameterName]),
	CONSTRAINT [FK_StepParameter_StepId] FOREIGN KEY ([StepId]) REFERENCES [biflow].[Step] ([StepId]) ON DELETE CASCADE,
    CONSTRAINT [CK_StepParameter_ParameterType] CHECK ([ParameterType] = 'Package' OR [ParameterType] = 'Base'),
    CONSTRAINT [CK_StepParameter_ParameterLevel] CHECK ([ParameterLevel] = 'Package' OR [ParameterLevel] = 'Project' OR [ParameterLevel] IS NULL),
	CONSTRAINT [CK_StepParameter_ParameterValueType] CHECK (
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
