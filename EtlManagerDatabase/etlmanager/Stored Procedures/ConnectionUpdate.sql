CREATE PROCEDURE [etlmanager].[ConnectionUpdate]
	@ConnectionId UNIQUEIDENTIFIER,
	@ConnectionName NVARCHAR(250),
	@ConnectionString NVARCHAR(MAX),
	@IsSensitive BIT = 0,
	@EncryptionKey NVARCHAR(128) = NULL
AS

UPDATE A
SET ConnectionName = @ConnectionName,
	ConnectionString = CASE WHEN @IsSensitive = 1 THEN NULL ELSE @ConnectionString END,
	IsSensitive = @IsSensitive,
	ConnectionStringEncrypted = CASE WHEN @IsSensitive = 1 THEN ENCRYPTBYPASSPHRASE(@EncryptionKey, @ConnectionString) ELSE NULL END
FROM etlmanager.Connection AS A
WHERE A.ConnectionId = @ConnectionId