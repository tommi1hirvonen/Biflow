CREATE TABLE [biflow].[Connection]
(
	[ConnectionId] UNIQUEIDENTIFIER NOT NULL,
	[ConnectionName] NVARCHAR(250) NOT NULL,
	[ConnectionString] NVARCHAR(MAX) NULL,
	[ConnectionType] VARCHAR(20) NOT NULL CONSTRAINT [DF_Connection_ConnectionType] DEFAULT ('Sql'),
	[ExecutePackagesAsLogin] NVARCHAR(128) NULL,
	CONSTRAINT [PK_Connection] PRIMARY KEY CLUSTERED ([ConnectionId]),
	CONSTRAINT [CK_Connection_ConnectionType] CHECK ([ConnectionType]='Sql' OR [ConnectionType]='AnalysisServices')
)
