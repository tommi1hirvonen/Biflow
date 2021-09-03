CREATE TABLE [etlmanager].[JobParameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[JobId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterType] VARCHAR(20) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
    CONSTRAINT [PK_JobParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_JobParameter] UNIQUE ([JobId],[ParameterName]),
	CONSTRAINT [FK_JobParameter_Job] FOREIGN KEY ([JobId]) REFERENCES [etlmanager].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [CK_JobParameter_ParameterType] CHECK (
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
