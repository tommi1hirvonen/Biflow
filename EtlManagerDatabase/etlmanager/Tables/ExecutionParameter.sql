CREATE TABLE [etlmanager].[ExecutionParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
	[ParameterType] VARCHAR(20) NOT NULL, 
    CONSTRAINT [PK_ExecutionParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [ParameterId]),
	CONSTRAINT [FK_ExecutionParameter_Execution] FOREIGN KEY ([ExecutionId]) REFERENCES [etlmanager].[Execution] ([ExecutionId]),
	CONSTRAINT [CK_ExecutionParameter_ParameterType] CHECK (
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
