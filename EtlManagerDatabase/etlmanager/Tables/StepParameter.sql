CREATE TABLE [etlmanager].[StepParameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterType] VARCHAR(20) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
	[JobParameterId] UNIQUEIDENTIFIER NULL CONSTRAINT [FK_StepParameter_JobParameter] FOREIGN KEY REFERENCES [etlmanager].[JobParameter] ([ParameterId]) ON DELETE SET NULL,
    CONSTRAINT [PK_StepParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_StepParameter] UNIQUE ([StepId],[ParameterName]),
	CONSTRAINT [FK_StepParameter_StepId] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId]) ON DELETE CASCADE,
	CONSTRAINT [CK_StepParameter_ParameterType] CHECK (
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
