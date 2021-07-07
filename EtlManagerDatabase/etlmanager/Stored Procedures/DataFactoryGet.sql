CREATE PROCEDURE [etlmanager].[DataFactoryGet]
	@DataFactoryId UNIQUEIDENTIFIER = NULL,
	@EncryptionKey NVARCHAR(128) = NULL
AS

SELECT
	DataFactoryId,
	DataFactoryName,
	TenantId,
	SubscriptionId,
	ClientId,
	ClientSecret = etlmanager.GetDecryptedValue(@EncryptionKey, ClientSecret),
	ResourceGroupName,
	ResourceName
FROM etlmanager.DataFactory
WHERE ISNULL(@DataFactoryId, DataFactoryId) = DataFactoryId