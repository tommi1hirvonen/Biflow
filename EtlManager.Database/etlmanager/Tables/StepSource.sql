CREATE TABLE [etlmanager].[StepSource]
(
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[DatabaseObjectId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_StepSource] PRIMARY KEY CLUSTERED ([StepId], [DatabaseObjectId]),
	CONSTRAINT [FK_StepSource_Step] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId]) ON DELETE CASCADE,
	CONSTRAINT [FK_StepSource_DatabaseObject] FOREIGN KEY ([DatabaseObjectId]) REFERENCES [etlmanager].[DatabaseObject] ([DatabaseObjectId]) ON DELETE CASCADE
)