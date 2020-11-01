CREATE FUNCTION [dbo].[GetConnectionStringDecrypted]
(
	@ConnectionId UNIQUEIDENTIFIER,
	@EncryptionKey NVARCHAR(128)
)
RETURNS NVARCHAR(MAX)
AS
BEGIN
	RETURN (
		SELECT
			CASE WHEN IsSensitive = 1 THEN CONVERT(NVARCHAR(MAX), DECRYPTBYPASSPHRASE(@EncryptionKey, ConnectionStringEncrypted))
			ELSE ConnectionString END
		FROM etlmanager.Connection
		WHERE ConnectionId = @ConnectionId
	)
END
