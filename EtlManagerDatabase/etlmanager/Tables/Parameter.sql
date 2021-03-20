CREATE TABLE [etlmanager].[Parameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
	[ParameterLevel] VARCHAR(20) NOT NULL,
	[ParameterType] VARCHAR(20) NOT NULL, 
    CONSTRAINT [PK_StepParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_Parameter] UNIQUE ([StepId],[ParameterName],[ParameterLevel]),
	CONSTRAINT [CK_ParameterLevel] CHECK ([ParameterLevel] IN ('Package','Project')),
	CONSTRAINT [FK_StepParameter_StepId] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId]) ON DELETE CASCADE
)
