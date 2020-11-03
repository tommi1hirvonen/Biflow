CREATE PROCEDURE [etlmanager].[DataFactoryUpdate]
	@DataFactoryId UNIQUEIDENTIFIER
	,@DataFactoryName NVARCHAR(250)
	,@TenantId NVARCHAR(36)
	,@SubscriptionId NVARCHAR(36)
	,@ClientId NVARCHAR(36)
	,@ClientSecret NVARCHAR(250)
	,@ResourceGroupName NVARCHAR(250)
	,@ResourceName NVARCHAR(250)
	,@EncryptionKey NVARCHAR(128)
AS

UPDATE A
SET DataFactoryName = @DataFactoryName,
	TenantId = @TenantId,
	SubscriptionId = @SubscriptionId,
	ClientId = @ClientId,
	ClientSecret = ENCRYPTBYPASSPHRASE(@EncryptionKey, @ClientSecret),
	ResourceGroupName = @ResourceGroupName,
	ResourceName = @ResourceName
FROM etlmanager.DataFactory AS A
WHERE A.DataFactoryId = @DataFactoryId