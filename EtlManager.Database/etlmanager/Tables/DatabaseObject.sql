CREATE TABLE [etlmanager].[DatabaseObject]
(
	[DatabaseObjectId] UNIQUEIDENTIFIER NOT NULL,
	[ServerName] VARCHAR(128) NOT NULL,
	[DatabaseName] VARCHAR(128) NOT NULL,
	[SchemaName] VARCHAR(128) NOT NULL,
	[ObjectName] VARCHAR(128) NOT NULL,
	[MaxConcurrentWrites] INT NOT NULL,
	CONSTRAINT [PK_DatabaseObject] PRIMARY KEY CLUSTERED ([DatabaseObjectId]),
	CONSTRAINT [UQ_DatabaseObject] UNIQUE ([ServerName], [DatabaseName], [SchemaName], [ObjectName])
)
