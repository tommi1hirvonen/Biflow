CREATE TABLE [etlmanager].[Parameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterValue] NVARCHAR(4000) NOT NULL,
	CONSTRAINT [PK_StepParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_Parameter] UNIQUE ([StepId],[ParameterName]),
	CONSTRAINT [FK_StepParameter_StepId] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId])
)
