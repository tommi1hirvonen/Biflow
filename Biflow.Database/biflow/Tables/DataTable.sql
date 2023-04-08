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
	[LockedColumns] VARCHAR(8000) NOT NULL CONSTRAINT [DF_DataTable_LockedColumns] DEFAULT ('[]'), -- Empty JSON array
	[AllowImport] BIT NOT NULL CONSTRAINT [DF_DataTable_AllowImport] DEFAULT (1),
    CONSTRAINT [PK_DataTable] PRIMARY KEY CLUSTERED ([DataTableId])
)
