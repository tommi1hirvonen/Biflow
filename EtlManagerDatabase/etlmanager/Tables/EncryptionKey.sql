CREATE TABLE [etlmanager].[EncryptionKey]
(
	[EncryptionId] NVARCHAR(50) NOT NULL,
	[EncryptionKey] VARBINARY(MAX) NOT NULL,
	[Entropy] VARBINARY(MAX) NOT NULL,
	CONSTRAINT [PK_EncryptionKey] PRIMARY KEY CLUSTERED ([EncryptionId]) 
)
