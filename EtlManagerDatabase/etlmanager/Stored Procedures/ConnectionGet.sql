CREATE PROCEDURE [etlmanager].[ConnectionGet]
	@ConnectionId UNIQUEIDENTIFIER = NULL,
	@EncryptionKey NVARCHAR(128) = NULL
AS

SELECT
	a.ConnectionId,
	a.ConnectionName,
	ConnectionString =
		CASE a.IsSensitive
			WHEN 1 THEN etlmanager.GetConnectionStringDecrypted(a.ConnectionId, @EncryptionKey)
			ELSE a.ConnectionString
		END,
	a.ExecutePackagesAsLogin,
	a.IsSensitive
FROM etlmanager.Connection AS a
WHERE ISNULL(@ConnectionId, a.ConnectionId) = a.ConnectionId