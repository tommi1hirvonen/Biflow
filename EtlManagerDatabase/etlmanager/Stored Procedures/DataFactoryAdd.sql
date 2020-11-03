CREATE PROCEDURE [etlmanager].[DataFactoryAdd]
	@DataFactoryName NVARCHAR(250)
	,@TenantId NVARCHAR(36)
	,@SubscriptionId NVARCHAR(36)
	,@ClientId NVARCHAR(36)
	,@ClientSecret NVARCHAR(250)
	,@ResourceGroupName NVARCHAR(250)
	,@ResourceName NVARCHAR(250)
	,@EncryptionKey NVARCHAR(128)
AS

INSERT INTO etlmanager.DataFactory (
	DataFactoryId,
	DataFactoryName,
	TenantId,
	SubscriptionId,
	ClientId,
	ClientSecret,
	ResourceGroupName,
	ResourceName
)
SELECT
	NEWID(),
	@DataFactoryName,
	@TenantId,
	@SubscriptionId,
	@ClientId,
	ENCRYPTBYPASSPHRASE(@EncryptionKey, @ClientSecret),
	@ResourceGroupName,
	@ResourceName