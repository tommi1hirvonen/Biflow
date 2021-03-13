CREATE PROCEDURE [etlmanager].[PowerBIServiceUpdate]
	@PowerBIServiceId UNIQUEIDENTIFIER
	,@PowerBIServiceName NVARCHAR(250)
	,@TenantId NVARCHAR(36)
	,@ClientId NVARCHAR(36)
	,@ClientSecret NVARCHAR(250)
	,@EncryptionKey NVARCHAR(128)
AS

UPDATE A
SET PowerBIServiceName = @PowerBIServiceName,
	TenantId = @TenantId,
	ClientId = @ClientId,
	ClientSecret = ENCRYPTBYPASSPHRASE(@EncryptionKey, @ClientSecret)
FROM etlmanager.PowerBIService AS A
WHERE A.PowerBIServiceId = @PowerBIServiceId