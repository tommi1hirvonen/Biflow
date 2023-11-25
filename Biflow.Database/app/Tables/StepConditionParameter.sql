CREATE TABLE [app].[StepConditionParameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
    [ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValueType] VARCHAR(20) NOT NULL,
	[ParameterValue] SQL_VARIANT NULL,
	[JobParameterId] UNIQUEIDENTIFIER NULL CONSTRAINT [FK_StepConditionParameter_JobParameter] FOREIGN KEY REFERENCES [app].[JobParameter] ([ParameterId]) ON DELETE SET NULL,
    CONSTRAINT [PK_StepConditionParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_StepConditionParameter] UNIQUE ([StepId], [ParameterName]),
	CONSTRAINT [FK_StepConditionParameter_StepId] FOREIGN KEY ([StepId]) REFERENCES [app].[Step] ([StepId]) ON DELETE CASCADE,
	CONSTRAINT [CK_StepConditionParameter_ParameterValueType] CHECK (
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
