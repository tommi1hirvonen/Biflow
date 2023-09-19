CREATE TABLE [biflow].[DataTable]
(
	[DataTableId] UNIQUEIDENTIFIER NOT NULL,
	[DataTableName] NVARCHAR(250) NOT NULL,
	[DataTableDescription] NVARCHAR(MAX) NULL,
	[TargetSchemaName] VARCHAR(128) NOT NULL,
	[TargetTableName] VARCHAR(128) NOT NULL,
	[ConnectionId] UNIQUEIDENTIFIER CONSTRAINT [FK_DataTable_Connection] FOREIGN KEY REFERENCES [biflow].[Connection] ([ConnectionId]) NOT NULL,
	[DataTableCategoryId] UNIQUEIDENTIFIER CONSTRAINT [FK_DataTable_Category] FOREIGN KEY REFERENCES [biflow].[DataTableCategory] ([DataTableCategoryId]) ON DELETE SET NULL,
	[Timestamp] ROWVERSION NOT NULL,
	[AllowInsert] BIT NOT NULL CONSTRAINT [DF_DataTable_AllowInsert] DEFAULT (1),
	[AllowDelete] BIT NOT NULL CONSTRAINT [DF_DataTable_AllowDelete] DEFAULT (1),
	[LockedColumns] VARCHAR(8000) NOT NULL,
	[AllowImport] BIT NOT NULL CONSTRAINT [DF_DataTable_AllowImport] DEFAULT (1),
	[AllowUpdate] BIT NOT NULL CONSTRAINT [DF_DataTable_AllowUpdate] DEFAULT (1),
	[LockedColumnsExcludeMode] BIT NOT NULL CONSTRAINT [DF_DataTable_LockedColumnsExcludeMode] DEFAULT (0),
    CONSTRAINT [PK_DataTable] PRIMARY KEY CLUSTERED ([DataTableId])
)

GO

CREATE TRIGGER [biflow].[Trigger_DataTable] ON [biflow].[DataTable] INSTEAD OF DELETE AS
BEGIN

	SET NOCOUNT ON 

	DELETE FROM a
	FROM [biflow].[DataTableLookup] AS a
		INNER JOIN [deleted] AS b ON a.[DataTableId] = b.[DataTableId]

	DELETE FROM a
	FROM [biflow].[DataTable] AS a
		INNER JOIN [deleted] AS b ON a.[DataTableId] = b.[DataTableId]

END