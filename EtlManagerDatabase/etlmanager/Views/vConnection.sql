CREATE VIEW [etlmanager].[vConnection] AS
SELECT
	ConnectionId,
	ConnectionName,
	CASE WHEN IsSensitive = 1 THEN 'Encrypted'
		ELSE ConnectionString
	END AS ConnectionString,
	IsSensitive,
	ExecutePackagesAsLogin
FROM etlmanager.Connection