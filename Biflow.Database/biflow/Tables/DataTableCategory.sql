CREATE TABLE [biflow].[DataTableCategory]
(
	[DataTableCategoryId] UNIQUEIDENTIFIER NOT NULL,
	[DataTableCategoryName] NVARCHAR(250) NOT NULL,
	CONSTRAINT [PK_DataTableCategory] PRIMARY KEY CLUSTERED ([DataTableCategoryId]),
	CONSTRAINT [UQ_DataTableCategory] UNIQUE ([DataTableCategoryName])
)
