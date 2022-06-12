CREATE TABLE [biflow].[PipelineClient]
(
	[PipelineClientId] UNIQUEIDENTIFIER NOT NULL,
	[PipelineClientName] NVARCHAR(250) NOT NULL,
	[PipelineClientType] VARCHAR(20) NOT NULL,
	[SubscriptionId] NVARCHAR(36) NULL,
	[ResourceGroupName] NVARCHAR(250) NULL,
	[ResourceName] NVARCHAR(250) NULL,
	[SynapseWorkspaceUrl] VARCHAR(500) NULL,
	[AppRegistrationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_DataFactory_AppRegistration] FOREIGN KEY REFERENCES biflow.AppRegistration ([AppRegistrationId]),
	CONSTRAINT [PK_PipelineClient] PRIMARY KEY CLUSTERED ([PipelineClientId]),
	CONSTRAINT [CK_PipelineClient_PipelineClientType] CHECK (
		[PipelineClientType] = 'DataFactory' AND [SubscriptionId] IS NOT NULL AND [ResourceGroupName] IS NOT NULL AND [ResourceName] IS NOT NULL
		OR [PipelineClientType] = 'Synapse' AND [SynapseWorkspaceUrl] IS NOT NULL
	),
)
