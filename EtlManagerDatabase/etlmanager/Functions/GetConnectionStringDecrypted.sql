CREATE FUNCTION [etlmanager].[GetConnectionStringDecrypted]
(
	@ConnectionId UNIQUEIDENTIFIER,
	@EncryptionKey NVARCHAR(128)
)
RETURNS NVARCHAR(MAX)
AS
BEGIN
	RETURN (
		SELECT
			CASE WHEN IsSensitive = 1 THEN etlmanager.GetDecryptedValue(@EncryptionKey, ConnectionStringEncrypted)
			ELSE ConnectionString END
		FROM etlmanager.Connection
		WHERE ConnectionId = @ConnectionId
	)
END
