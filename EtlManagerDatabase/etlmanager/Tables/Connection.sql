CREATE TABLE [etlmanager].[Connection]
(
	[ConnectionId] UNIQUEIDENTIFIER NOT NULL,
	[ConnectionName] NVARCHAR(250) NOT NULL,
	[ConnectionString] NVARCHAR(2000) NOT NULL,
	CONSTRAINT [PK_Connection] PRIMARY KEY CLUSTERED ([ConnectionId])
)
