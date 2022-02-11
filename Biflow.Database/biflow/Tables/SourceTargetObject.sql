CREATE TABLE [biflow].[SourceTargetObject]
(
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	[ServerName] VARCHAR(128) NOT NULL,
	[DatabaseName] VARCHAR(128) NOT NULL,
	[SchemaName] VARCHAR(128) NOT NULL,
	[ObjectName] VARCHAR(128) NOT NULL,
	[MaxConcurrentWrites] INT NOT NULL,
	CONSTRAINT [PK_SourceTargetObject] PRIMARY KEY CLUSTERED ([ObjectId]),
	CONSTRAINT [UQ_SourceTargetObject] UNIQUE ([ServerName], [DatabaseName], [SchemaName], [ObjectName])
)
