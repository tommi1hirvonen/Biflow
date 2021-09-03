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
	CONSTRAINT [FK_ExecutionStepParameter_ExecutionStep] FOREIGN KEY ([ExecutionId], [StepId]) REFERENCES [etlmanager].[ExecutionStep] ([ExecutionId], [StepId])
)
