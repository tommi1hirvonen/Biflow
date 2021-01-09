CREATE TABLE [etlmanager].[ExecutionParameter]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValue] NVARCHAR(4000) NOT NULL,
	CONSTRAINT [PK_ExecutionParameter] PRIMARY KEY CLUSTERED ([ExecutionId], [ParameterId])
)
