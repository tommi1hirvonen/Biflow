CREATE TABLE [biflow].[DataObject]
(
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	[ServerName] VARCHAR(128) NOT NULL,
	[DatabaseName] VARCHAR(128) NOT NULL,
	[SchemaName] VARCHAR(128) NOT NULL,
	[ObjectName] VARCHAR(128) NOT NULL,
	[MaxConcurrentWrites] INT NOT NULL,
	CONSTRAINT [PK_DataObject] PRIMARY KEY CLUSTERED ([ObjectId]),
	CONSTRAINT [UQ_DataObject] UNIQUE ([ServerName], [DatabaseName], [SchemaName], [ObjectName])
)
