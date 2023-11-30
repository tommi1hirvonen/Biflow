CREATE TABLE [app].[ExecutionDataObject]
(
	[ExecutionId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectUri] VARCHAR(500) NOT NULL,
	[MaxConcurrentWrites] INT NOT NULL,
	CONSTRAINT [PK_ExecutionDataObject] PRIMARY KEY CLUSTERED ([ExecutionId], [ObjectId]),
	CONSTRAINT [FK_ExecutionDataObject_Execution] FOREIGN KEY ([ExecutionId]) REFERENCES [app].[Execution] ([ExecutionId]) ON DELETE CASCADE
)
