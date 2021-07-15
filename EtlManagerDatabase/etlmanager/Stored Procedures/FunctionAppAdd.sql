CREATE PROCEDURE [etlmanager].[FunctionAppAdd]
	@FunctionAppName NVARCHAR(250)
	,@FunctionAppUrl NVARCHAR(36)
	,@FunctionAppKey NVARCHAR(250)
	,@EncryptionKey NVARCHAR(128)
AS

INSERT INTO etlmanager.FunctionApp(
	FunctionAppId,
	FunctionAppName,
	FunctionAppUrl,
	FunctionAppKey
)
SELECT
	NEWID(),
	@FunctionAppName,
	@FunctionAppUrl,
	ENCRYPTBYPASSPHRASE(@EncryptionKey, @FunctionAppKey)