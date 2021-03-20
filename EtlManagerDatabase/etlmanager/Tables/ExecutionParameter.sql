CREATE TABLE [etlmanager].[ExecutionParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
	[ParameterLevel] VARCHAR(20) NOT NULL,
	[ParameterType] VARCHAR(20) NOT NULL, 
    CONSTRAINT [PK_ExecutionParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [ParameterId])
)
