CREATE TABLE [biflow].[DataTableLookup]
(
	[DataTableId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_DataTableLookup_DataTable] FOREIGN KEY REFERENCES [biflow].[DataTable] ([DataTableId]),
	[ColumnName] NVARCHAR(128) NOT NULL,
	[LookupDataTableId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_DataTableLookup_LookupDataTable] FOREIGN KEY REFERENCES [biflow].[DataTable] ([DataTableId]),
	[LookupValueColumn] NVARCHAR(128) NOT NULL,
	[LookupDescriptionColumn] NVARCHAR(128) NOT NULL,
	[LookupDisplayType] VARCHAR(20) NOT NULL,
	CONSTRAINT [PK_DataTableLookup] PRIMARY KEY CLUSTERED ([DataTableId], [ColumnName]),
	CONSTRAINT [CK_DataTableLookup] CHECK ([LookupDisplayType] IN ('Value', 'Description', 'ValueAndDescription'))
)
