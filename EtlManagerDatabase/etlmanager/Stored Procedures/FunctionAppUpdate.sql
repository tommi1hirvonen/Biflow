CREATE PROCEDURE [etlmanager].[FunctionAppUpdate]
	@FunctionAppId UNIQUEIDENTIFIER
	,@FunctionAppName NVARCHAR(250)
	,@FunctionAppUrl NVARCHAR(36)
	,@FunctionAppKey NVARCHAR(250)
	,@EncryptionKey NVARCHAR(128)
AS

UPDATE A
SET FunctionAppName = @FunctionAppName,
	FunctionAppUrl = @FunctionAppUrl,
	FunctionAppKey = ENCRYPTBYPASSPHRASE(@EncryptionKey, @FunctionAppKey)
FROM etlmanager.FunctionApp AS A
WHERE A.FunctionAppId = @FunctionAppId