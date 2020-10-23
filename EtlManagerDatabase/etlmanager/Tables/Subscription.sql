CREATE TABLE [etlmanager].[Subscription]
(
	[SubscriptionId]	UNIQUEIDENTIFIER	NOT NULL,
	[JobId]				UNIQUEIDENTIFIER	NOT NULL,
	[Username]			NVARCHAR (250)		NOT NULL,
	CONSTRAINT [PK_Subscription] PRIMARY KEY CLUSTERED ([SubscriptionId]),
	CONSTRAINT [FK_Subscription_JobId] FOREIGN KEY ([JobId]) REFERENCES [etlmanager].[Job] ([JobId]),
	CONSTRAINT [FK_Subscription_Username] FOREIGN KEY ([Username]) REFERENCES [etlmanager].[User] ([Username]),
	CONSTRAINT [UQ_Subscription] UNIQUE ([JobId], [Username])
)
