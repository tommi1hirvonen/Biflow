CREATE TABLE [etlmanager].[PackageParameter]
(
	[ParameterId] UNIQUEIDENTIFIER NOT NULL,
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ParameterLevel] VARCHAR(20) NOT NULL,
	[ParameterName] NVARCHAR(128) NOT NULL,
	[ParameterType] VARCHAR(20) NOT NULL,
	[ParameterValue] SQL_VARIANT NOT NULL,
    CONSTRAINT [PK_PackageParameter] PRIMARY KEY CLUSTERED ([ParameterId]),
	CONSTRAINT [UQ_PackageParameter] UNIQUE ([StepId],[ParameterName],[ParameterLevel]),
	CONSTRAINT [CK_PackageParameter_ParameterLevel] CHECK ([ParameterLevel] = 'Package' OR [ParameterLevel] = 'Project'),
	CONSTRAINT [FK_PackageParameter_StepId] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId]) ON DELETE CASCADE
)
