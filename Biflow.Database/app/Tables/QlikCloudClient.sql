CREATE TABLE [app].[QlikCloudClient]
(
	[QlikCloudClientId]		UNIQUEIDENTIFIER	NOT NULL,
	[QlikCloudClientName]	NVARCHAR(250)		NOT NULL,
	[EnvironmentUrl]		NVARCHAR(4000)		NOT NULL,
	[ApiToken]				NVARCHAR(4000)		NOT NULL,
	CONSTRAINT [PK_QlikCloudClient] PRIMARY KEY CLUSTERED ([QlikCloudClientId])
)
