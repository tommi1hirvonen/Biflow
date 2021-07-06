CREATE TABLE [etlmanager].[Subscription]
(
	[SubscriptionId]	UNIQUEIDENTIFIER	NOT NULL,
	[JobId]				UNIQUEIDENTIFIER	NOT NULL,
	[Username]			NVARCHAR (250)		NOT NULL,
	[SubscriptionType]  VARCHAR(20)			NOT NULL,
	CONSTRAINT [PK_Subscription] PRIMARY KEY CLUSTERED ([SubscriptionId]),
	CONSTRAINT [FK_Subscription_JobId] FOREIGN KEY ([JobId]) REFERENCES [etlmanager].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [FK_Subscription_Username] FOREIGN KEY ([Username]) REFERENCES [etlmanager].[User] ([Username]) ON DELETE CASCADE,
	CONSTRAINT [UQ_Subscription] UNIQUE ([JobId], [Username]),
	CONSTRAINT [CK_Subscription_SubscriptionType] CHECK ([SubscriptionType] = 'FAILURE' OR [SubscriptionType] = 'SUCCESS' OR [SubscriptionType] = 'COMPLETION')
)
