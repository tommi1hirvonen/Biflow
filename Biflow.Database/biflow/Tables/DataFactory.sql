CREATE TABLE [biflow].[DataFactory]
(
	[DataFactoryId] UNIQUEIDENTIFIER NOT NULL,
	[DataFactoryName] NVARCHAR(250) NOT NULL,
	[SubscriptionId] NVARCHAR(36) NOT NULL,
	[ResourceGroupName] NVARCHAR(250) NOT NULL,
	[ResourceName] NVARCHAR(250) NOT NULL,
	[AppRegistrationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_DataFactory_AppRegistration] FOREIGN KEY REFERENCES biflow.AppRegistration ([AppRegistrationId]),
	CONSTRAINT [PK_DataFactory] PRIMARY KEY CLUSTERED ([DataFactoryId])
)
