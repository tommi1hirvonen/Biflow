CREATE PROCEDURE [dbo].[ConnectionAdd]
	@ConnectionName NVARCHAR(250),
	@ConnectionString NVARCHAR(MAX),
	@IsSensitive BIT = 0,
	@EncryptionKey NVARCHAR(128) = NULL
AS

INSERT INTO etlmanager.Connection (
	ConnectionId,
	ConnectionName,
	ConnectionString,
	IsSensitive,
	ConnectionStringEncrypted
)
SELECT
	NEWID(),
	@ConnectionName,
	CASE WHEN @IsSensitive = 1 THEN NULL ELSE @ConnectionString END,
	@IsSensitive,
	CASE WHEN @IsSensitive = 1 THEN ENCRYPTBYPASSPHRASE(@EncryptionKey, @ConnectionString) ELSE NULL END