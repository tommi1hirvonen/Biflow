CREATE TABLE [app].[DataTableLookup]
(
	[LookupId] UNIQUEIDENTIFIER NOT NULL,
	[DataTableId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_DataTableLookup_DataTable] FOREIGN KEY REFERENCES [app].[DataTable] ([DataTableId]),
	[ColumnName] NVARCHAR(128) NOT NULL,
	[LookupDataTableId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_DataTableLookup_LookupDataTable] FOREIGN KEY REFERENCES [app].[DataTable] ([DataTableId]),
	[LookupValueColumn] NVARCHAR(128) NOT NULL,
	[LookupDescriptionColumn] NVARCHAR(128) NOT NULL,
	[LookupDisplayType] VARCHAR(20) NOT NULL,
	CONSTRAINT [PK_DataTableLookup] PRIMARY KEY CLUSTERED ([LookupId]),
	CONSTRAINT [UQ_DataTableLookup] UNIQUE ([DataTableId], [ColumnName]),
	CONSTRAINT [CK_DataTableLookup] CHECK (
		[LookupDisplayType] = 'Value' OR
		[LookupDisplayType] = 'Description' OR
		[LookupDisplayType] = 'ValueAndDescription')
)
