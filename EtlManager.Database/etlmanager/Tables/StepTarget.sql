CREATE TABLE [etlmanager].[StepTarget]
(
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[DatabaseObjectId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_StepTarget] PRIMARY KEY CLUSTERED ([StepId], [DatabaseObjectId]),
	CONSTRAINT [FK_StepTarget_Step] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId]) ON DELETE CASCADE,
	CONSTRAINT [FK_StepTarget_DatabaseObject] FOREIGN KEY ([DatabaseObjectId]) REFERENCES [etlmanager].[DatabaseObject] ([DatabaseObjectId]) ON DELETE CASCADE
)