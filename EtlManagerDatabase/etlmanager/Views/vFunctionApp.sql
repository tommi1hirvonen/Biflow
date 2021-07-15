CREATE VIEW [etlmanager].[vFunctionApp] AS
SELECT
	FunctionAppId,
	FunctionAppName,
	FunctionAppUrl,
	FunctionAppKey = 'Encrypted'
FROM etlmanager.FunctionApp