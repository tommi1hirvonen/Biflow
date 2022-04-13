CREATE TABLE [biflow].[DataTable]
(
	[DataTableId] UNIQUEIDENTIFIER NOT NULL,
	[DataTableName] NVARCHAR(250) NOT NULL,
	[DataTableDescription] NVARCHAR(MAX) NULL,
	[TargetSchemaName] VARCHAR(128) NOT NULL,
	[TargetTableName] VARCHAR(128) NOT NULL,
	[ConnectionId] UNIQUEIDENTIFIER CONSTRAINT [FK_DataTable_Connection] FOREIGN KEY REFERENCES [biflow].[Connection] ([ConnectionId]) NOT NULL,
	CONSTRAINT [PK_DataTable] PRIMARY KEY CLUSTERED ([DataTableId])

)
