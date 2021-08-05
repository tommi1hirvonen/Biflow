CREATE TABLE [etlmanager].[AppRegistration]
(
	[AppRegistrationId] UNIQUEIDENTIFIER NOT NULL,
	[AppRegistrationName] NVARCHAR(250) NOT NULL,
	[TenantId] NVARCHAR(36) NOT NULL,
	[ClientId] NVARCHAR(36) NOT NULL,
	[ClientSecret] VARCHAR(1000) NOT NULL,
	[AccessToken] NVARCHAR(MAX) NULL,
	[AccessTokenExpiresOn] DATETIME2 NULL,
	CONSTRAINT [PK_AppRegistration] PRIMARY KEY CLUSTERED ([AppRegistrationId])
)
