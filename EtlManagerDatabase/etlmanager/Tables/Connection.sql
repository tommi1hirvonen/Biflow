CREATE TABLE [etlmanager].[Connection]
(
	[ConnectionId] UNIQUEIDENTIFIER NOT NULL,
	[ConnectionName] NVARCHAR(250) NOT NULL,
	[ConnectionString] NVARCHAR(MAX) NULL,
	[ConnectionStringEncrypted] VARBINARY(MAX) NULL,
	[IsSensitive] BIT NOT NULL CONSTRAINT [DF_Connection_Sensitive] DEFAULT (0),
	[ExecutePackagesAsLogin] NVARCHAR(128) NULL,
	CONSTRAINT [PK_Connection] PRIMARY KEY CLUSTERED ([ConnectionId]),
	CONSTRAINT [CK_Connection] CHECK ([IsSensitive] = 0 AND [ConnectionString] IS NOT NULL OR [IsSensitive] = 1 AND [ConnectionStringEncrypted] IS NOT NULL)
)
