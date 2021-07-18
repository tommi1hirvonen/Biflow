CREATE TABLE [etlmanager].[DataFactory]
(
	[DataFactoryId] UNIQUEIDENTIFIER NOT NULL,
	[DataFactoryName] NVARCHAR(250) NOT NULL,
	[TenantId] NVARCHAR(36) NOT NULL,
	[SubscriptionId] NVARCHAR(36) NOT NULL,
	[ClientId] NVARCHAR(36) NOT NULL,
	[ClientSecret] VARCHAR(1000) NOT NULL,
	[ResourceGroupName] NVARCHAR(250) NOT NULL,
	[ResourceName] NVARCHAR(250) NOT NULL,
	[AccessToken] NVARCHAR(MAX) NULL,
	[AccessTokenExpiresOn] DATETIME2 NULL,
	CONSTRAINT [PK_DataFactory] PRIMARY KEY CLUSTERED ([DataFactoryId])
)
