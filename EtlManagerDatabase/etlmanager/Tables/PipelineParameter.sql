CREATE TABLE [etlmanager].[PipelineParameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterType] VARCHAR(20) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
    CONSTRAINT [PK_PipelineParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_PipelineParameter] UNIQUE ([StepId],[ParameterName]),
	CONSTRAINT [FK_PipelineParameter_StepId] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId]) ON DELETE CASCADE
)
