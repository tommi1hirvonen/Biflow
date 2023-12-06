CREATE TABLE [app].[BlobStorageClient]
(
	[BlobStorageClientId] UNIQUEIDENTIFIER NOT NULL,
	[BlobStorageClientName] NVARCHAR(250) NOT NULL,
	[ConnectionMethod] VARCHAR(20) NOT NULL,
	[StorageAccountUrl] NVARCHAR(4000) NULL,
	[ConnectionString] NVARCHAR(4000) NULL,
	[AppRegistrationId] UNIQUEIDENTIFIER NULL CONSTRAINT [FK_BlobStorageClient_AppRegistration] FOREIGN KEY REFERENCES [app].[AppRegistration] ([AppRegistrationId]) ON DELETE CASCADE,
	CONSTRAINT [PK_BlobStorageClient] PRIMARY KEY CLUSTERED ([BlobStorageClientId] ASC),
	CONSTRAINT [CK_BlobStorageClient] CHECK (
		-- use connection string
		([ConnectionMethod] = 'ConnectionString' AND [ConnectionString] IS NOT NULL AND [StorageAccountUrl] IS NULL AND [AppRegistrationId] IS NULL)
		-- use app registration together with account url
		OR ([ConnectionMethod] = 'AppRegistration' AND [AppRegistrationId] IS NOT NULL AND [StorageAccountUrl] IS NOT NULL AND [ConnectionString] IS NULL) 
		-- use account url with embedded SAS token
		OR ([ConnectionMethod] = 'Url' AND [StorageAccountUrl] IS NOT NULL AND [ConnectionString] IS NULL AND [AppRegistrationId] IS NULL)
	)
)
