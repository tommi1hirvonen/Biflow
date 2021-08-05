CREATE TABLE [etlmanager].[FunctionApp]
(
	[FunctionAppId] UNIQUEIDENTIFIER NOT NULL,
	[FunctionAppName] NVARCHAR(250) NOT NULL,
	[FunctionAppKey] VARCHAR(1000) NULL,
	[TenantId] NVARCHAR(36) NOT NULL,
	[SubscriptionId] NVARCHAR(36) NOT NULL,
	[ClientId] NVARCHAR(36) NOT NULL,
	[ClientSecret] VARCHAR(1000) NOT NULL,
	[ResourceGroupName] NVARCHAR(250) NOT NULL,
	[ResourceName] NVARCHAR(250) NOT NULL,
	[AccessToken] NVARCHAR(MAX) NULL,
	[AccessTokenExpiresOn] DATETIME2 NULL,
	CONSTRAINT [PK_FunctionApp] PRIMARY KEY CLUSTERED ([FunctionAppId] ASC)
)
