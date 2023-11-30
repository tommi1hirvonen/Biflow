CREATE TABLE [app].[DataObject]
(
	[ObjectId] UNIQUEIDENTIFIER NOT NULL,
	[ObjectUri] VARCHAR(500) NOT NULL,
	[MaxConcurrentWrites] INT NOT NULL,
	CONSTRAINT [PK_DataObject] PRIMARY KEY CLUSTERED ([ObjectId]),
	CONSTRAINT [UQ_DataObject] UNIQUE ([ObjectUri])
)
