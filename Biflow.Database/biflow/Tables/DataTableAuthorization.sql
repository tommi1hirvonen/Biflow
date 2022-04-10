CREATE TABLE [biflow].[DataTableAuthorization]
(
	[DataTableId] UNIQUEIDENTIFIER NOT NULL,
	[Username] NVARCHAR (250) NOT NULL,
	CONSTRAINT [PK_DataTableAuthorization] PRIMARY KEY CLUSTERED ([DataTableId], [Username]),
	CONSTRAINT [FK_DataTableAuthorization_DataTable] FOREIGN KEY ([DataTableId]) REFERENCES [biflow].[DataTable] ([DataTableId]) ON DELETE CASCADE,
	CONSTRAINT [FK_DataTableAuthorization_User] FOREIGN KEY ([Username]) REFERENCES [biflow].[User] ([Username]) ON DELETE CASCADE
)
