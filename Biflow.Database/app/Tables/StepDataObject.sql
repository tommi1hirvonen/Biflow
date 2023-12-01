CREATE TABLE [app].[StepDataObject]
(
	[StepId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	[ReferenceType] VARCHAR(20) NOT NULL,
	[DataAttributes] NVARCHAR(4000) NOT NULL CONSTRAINT [DF_StepDataObject_DataAttributes] DEFAULT ('[]'), -- empty json array
	CONSTRAINT [PK_StepDataObject] PRIMARY KEY CLUSTERED ([StepId], [ObjectId], [ReferenceType]),
	CONSTRAINT [FK_StepDataObject_Step] FOREIGN KEY ([StepId]) REFERENCES [app].[Step] ([StepId]) ON DELETE CASCADE,
	CONSTRAINT [FK_StepDataObject_DataObject] FOREIGN KEY ([ObjectId]) REFERENCES [app].[DataObject] ([ObjectId]) ON DELETE CASCADE,
	CONSTRAINT [CK_StepDataObject_ReferenceType] CHECK ([ReferenceType] = 'Source' OR [ReferenceType] = 'Target')
)