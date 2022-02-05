CREATE TABLE [etlmanager].[StepTarget]
(
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_StepTarget] PRIMARY KEY CLUSTERED ([StepId], [ObjectId]),
	CONSTRAINT [FK_StepTarget_Step] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId]) ON DELETE CASCADE,
	CONSTRAINT [FK_StepTarget_SourceTargetObject] FOREIGN KEY ([ObjectId]) REFERENCES [etlmanager].[SourceTargetObject] ([ObjectId]) ON DELETE CASCADE
)