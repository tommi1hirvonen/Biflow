CREATE PROCEDURE [etlmanager].[PowerBIServiceGet]
	@PowerBIServiceId UNIQUEIDENTIFIER = NULL,
	@EncryptionKey NVARCHAR(128) = NULL
AS

SELECT
	PowerBIServiceId,
	PowerBIServiceName,
	TenantId,
	ClientId,
	ClientSecret = etlmanager.GetDecryptedValue(@EncryptionKey, ClientSecret)
FROM etlmanager.PowerBIService
WHERE ISNULL(@PowerBIServiceId, PowerBIServiceId) = PowerBIServiceId