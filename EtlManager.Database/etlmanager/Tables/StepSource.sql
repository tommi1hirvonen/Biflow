CREATE TABLE [etlmanager].[StepSource]
(
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_StepSource] PRIMARY KEY CLUSTERED ([StepId], [ObjectId]),
	CONSTRAINT [FK_StepSource_Step] FOREIGN KEY ([StepId]) REFERENCES [etlmanager].[Step] ([StepId]) ON DELETE CASCADE,
	CONSTRAINT [FK_StepSource_SourceTargetObject] FOREIGN KEY ([ObjectId]) REFERENCES [etlmanager].[SourceTargetObject] ([ObjectId]) ON DELETE CASCADE
)