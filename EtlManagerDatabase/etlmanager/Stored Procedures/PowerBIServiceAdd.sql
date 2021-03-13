CREATE PROCEDURE [etlmanager].[PowerBIServiceAdd]
	@PowerBIServiceName NVARCHAR(250)
	,@TenantId NVARCHAR(36)
	,@ClientId NVARCHAR(36)
	,@ClientSecret NVARCHAR(250)
	,@EncryptionKey NVARCHAR(128)
AS

INSERT INTO etlmanager.PowerBIService (
	PowerBIServiceId,
	PowerBIServiceName,
	TenantId,
	ClientId,
	ClientSecret
)
SELECT
	NEWID(),
	@PowerBIServiceName,
	@TenantId,
	@ClientId,
	ENCRYPTBYPASSPHRASE(@EncryptionKey, @ClientSecret)