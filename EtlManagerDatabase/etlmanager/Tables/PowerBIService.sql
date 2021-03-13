CREATE TABLE [etlmanager].[PowerBIService]
(
	[PowerBIServiceId] UNIQUEIDENTIFIER NOT NULL,
	[PowerBIServiceName] NVARCHAR(250) NOT NULL,
	[TenantId] NVARCHAR(36) NOT NULL,
	[ClientId] NVARCHAR(36) NOT NULL,
	[ClientSecret] VARBINARY(MAX) NOT NULL,
	[AccessToken] NVARCHAR(MAX) NULL,
	[AccessTokenExpiresOn] DATETIME2 NULL,
	CONSTRAINT [PK_PowerBIService] PRIMARY KEY CLUSTERED ([PowerBIServiceId])
)
