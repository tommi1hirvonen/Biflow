CREATE TABLE [biflow].[Subscription]
(
	[JobId]				UNIQUEIDENTIFIER	NOT NULL,
	[Username]			NVARCHAR (250)		NOT NULL,
	[SubscriptionType]  VARCHAR(20)			NULL,
	[NotifyOnOvertime]	BIT					CONSTRAINT [DF_Subscription_NotifyOnOvertime] DEFAULT (0) NOT NULL,
	CONSTRAINT [PK_Subscription] PRIMARY KEY CLUSTERED ([JobId], [Username]),
	CONSTRAINT [FK_Subscription_JobId] FOREIGN KEY ([JobId]) REFERENCES [biflow].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [FK_Subscription_Username] FOREIGN KEY ([Username]) REFERENCES [biflow].[User] ([Username]) ON DELETE CASCADE,
	CONSTRAINT [CK_Subscription_SubscriptionType] CHECK ([SubscriptionType] = 'OnFailure' OR [SubscriptionType] = 'OnSuccess' OR [SubscriptionType] = 'OnCompletion')
)
