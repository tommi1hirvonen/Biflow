CREATE TABLE [app].[DataTable]
(
	[DataTableId] UNIQUEIDENTIFIER NOT NULL,
	[DataTableName] NVARCHAR(250) NOT NULL,
	[DataTableDescription] NVARCHAR(MAX) NULL,
	[TargetSchemaName] VARCHAR(128) NOT NULL,
	[TargetTableName] VARCHAR(128) NOT NULL,
	[ConnectionId] UNIQUEIDENTIFIER CONSTRAINT [FK_DataTable_Connection] FOREIGN KEY REFERENCES [app].[Connection] ([ConnectionId]) NOT NULL,
	[DataTableCategoryId] UNIQUEIDENTIFIER CONSTRAINT [FK_DataTable_Category] FOREIGN KEY REFERENCES [app].[DataTableCategory] ([DataTableCategoryId]) ON DELETE SET NULL,
	[Timestamp] ROWVERSION NOT NULL,
	[AllowInsert] BIT NOT NULL CONSTRAINT [DF_DataTable_AllowInsert] DEFAULT (1),
	[AllowDelete] BIT NOT NULL CONSTRAINT [DF_DataTable_AllowDelete] DEFAULT (1),
	[LockedColumns] VARCHAR(8000) NOT NULL CONSTRAINT [DF_DataTable_LockedColumns] DEFAULT ('[]'), -- Empty JSON array
	[AllowImport] BIT NOT NULL CONSTRAINT [DF_DataTable_AllowImport] DEFAULT (1),
	[AllowUpdate] BIT NOT NULL CONSTRAINT [DF_DataTable_AllowUpdate] DEFAULT (1),
	[LockedColumnsExcludeMode] BIT NOT NULL CONSTRAINT [DF_DataTable_LockedColumnsExcludeMode] DEFAULT (0),
	[HiddenColumns] VARCHAR(8000) NOT NULL CONSTRAINT [DF_DataTable_HiddenColumns] DEFAULT ('[]'), -- Empty JSON array
	[ColumnOrder] VARCHAR(8000) NOT NULL CONSTRAINT [DF_DataTable_ColumnOrder] DEFAULT ('[]'),
    CONSTRAINT [PK_DataTable] PRIMARY KEY CLUSTERED ([DataTableId])
)

GO

CREATE TRIGGER [app].[Trigger_DataTable] ON [app].[DataTable] INSTEAD OF DELETE AS
BEGIN

	SET NOCOUNT ON 

	DELETE FROM a
	FROM [app].[DataTableLookup] AS a
		INNER JOIN [deleted] AS b ON a.[DataTableId] = b.[DataTableId]

	DELETE FROM a
	FROM [app].[DataTable] AS a
		INNER JOIN [deleted] AS b ON a.[DataTableId] = b.[DataTableId]

END