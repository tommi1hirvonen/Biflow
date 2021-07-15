CREATE PROCEDURE [etlmanager].[FunctionAppGet]
	@FunctionAppId UNIQUEIDENTIFIER = NULL,
	@EncryptionKey NVARCHAR(128) = NULL
AS

SELECT
	FunctionAppId,
	FunctionAppName,
	FunctionAppUrl,
	FunctionAppKey = etlmanager.GetDecryptedValue(@EncryptionKey, FunctionAppKey)
FROM etlmanager.FunctionApp
WHERE ISNULL(@FunctionAppId, FunctionAppId) = FunctionAppId