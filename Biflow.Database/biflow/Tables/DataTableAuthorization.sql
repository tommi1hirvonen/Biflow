CREATE TABLE [biflow].[DataTableAuthorization]
(
	[DataTableId] UNIQUEIDENTIFIER NOT NULL,
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_DataTableAuthorization] PRIMARY KEY CLUSTERED ([DataTableId], [UserId]),
	CONSTRAINT [FK_DataTableAuthorization_DataTable] FOREIGN KEY ([DataTableId]) REFERENCES [biflow].[DataTable] ([DataTableId]) ON DELETE CASCADE,
	CONSTRAINT [FK_DataTableAuthorization_User] FOREIGN KEY ([UserId]) REFERENCES [biflow].[User] ([UserId]) ON DELETE CASCADE
)
