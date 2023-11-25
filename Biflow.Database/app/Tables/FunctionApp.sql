CREATE TABLE [app].[FunctionApp]
(
	[FunctionAppId] UNIQUEIDENTIFIER NOT NULL,
	[FunctionAppName] NVARCHAR(250) NOT NULL,
	[FunctionAppKey] VARCHAR(1000) NULL,
	[SubscriptionId] NVARCHAR(36) NOT NULL,
	[ResourceGroupName] NVARCHAR(250) NOT NULL,
	[ResourceName] NVARCHAR(250) NOT NULL,
	[AppRegistrationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_FunctionApp_AppRegistration] FOREIGN KEY REFERENCES [app].[AppRegistration] ([AppRegistrationId]),
	CONSTRAINT [PK_FunctionApp] PRIMARY KEY CLUSTERED ([FunctionAppId] ASC)
)
