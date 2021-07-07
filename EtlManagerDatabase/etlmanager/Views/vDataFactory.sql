CREATE VIEW [etlmanager].[vDataFactory] AS
SELECT
	DataFactoryId,
	DataFactoryName,
	TenantId,
	SubscriptionId,
	ClientId,
	ClientSecret = 'Encrypted',
	ResourceGroupName,
	ResourceName
FROM etlmanager.DataFactory