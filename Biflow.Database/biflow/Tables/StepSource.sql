CREATE TABLE [biflow].[StepSource]
(
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_StepSource] PRIMARY KEY CLUSTERED ([StepId], [ObjectId]),
	CONSTRAINT [FK_StepSource_Step] FOREIGN KEY ([StepId]) REFERENCES [biflow].[Step] ([StepId]) ON DELETE CASCADE,
	CONSTRAINT [FK_StepSource_DataObject] FOREIGN KEY ([ObjectId]) REFERENCES [biflow].[DataObject] ([ObjectId]) ON DELETE CASCADE
)