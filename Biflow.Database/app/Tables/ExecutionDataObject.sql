CREATE TABLE [app].[ExecutionDataObject]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	[ServerName] VARCHAR(128) NOT NULL,
	[DatabaseName] VARCHAR(128) NOT NULL,
	[SchemaName] VARCHAR(128) NOT NULL,
	[ObjectName] VARCHAR(128) NOT NULL,
	[MaxConcurrentWrites] INT NOT NULL,
	CONSTRAINT [PK_ExecutionDataObject] PRIMARY KEY CLUSTERED ([ExecutionId], [ObjectId]),
	CONSTRAINT [FK_ExecutionDataObject_Execution] FOREIGN KEY ([ExecutionId]) REFERENCES [app].[Execution] ([ExecutionId]) ON DELETE CASCADE
)
