CREATE TABLE [etlmanager].[JobParameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[JobId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValueType] VARCHAR(20) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
    CONSTRAINT [PK_JobParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_JobParameter] UNIQUE ([JobId],[ParameterName]),
	CONSTRAINT [FK_JobParameter_Job] FOREIGN KEY ([JobId]) REFERENCES [etlmanager].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [CK_JobParameter_ParameterValueType] CHECK (
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
