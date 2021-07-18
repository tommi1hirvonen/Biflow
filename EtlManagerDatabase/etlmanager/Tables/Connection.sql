CREATE TABLE [etlmanager].[Connection]
(
	[ConnectionId] UNIQUEIDENTIFIER NOT NULL,
	[ConnectionName] NVARCHAR(250) NOT NULL,
	[ConnectionString] NVARCHAR(MAX) NULL,
	[ExecutePackagesAsLogin] NVARCHAR(128) NULL,
	CONSTRAINT [PK_Connection] PRIMARY KEY CLUSTERED ([ConnectionId])
)
