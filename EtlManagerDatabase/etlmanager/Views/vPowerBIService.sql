CREATE VIEW [etlmanager].[vPowerBIService] AS
SELECT
	PowerBIServiceId,
	PowerBIServiceName,
	TenantId,
	ClientId,
	ClientSecret = 'Encrypted'
FROM etlmanager.PowerBIService