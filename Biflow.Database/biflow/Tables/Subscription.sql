CREATE TABLE [biflow].[Subscription]
(
	[JobId]				UNIQUEIDENTIFIER	NOT NULL,
	[UserId]			UNIQUEIDENTIFIER    NOT NULL,
	[SubscriptionType]  VARCHAR(20)			NULL,
	[NotifyOnOvertime]	BIT					CONSTRAINT [DF_Subscription_NotifyOnOvertime] DEFAULT (0) NOT NULL,
	CONSTRAINT [PK_Subscription] PRIMARY KEY CLUSTERED ([JobId], [UserId]),
	CONSTRAINT [FK_Subscription_Job] FOREIGN KEY ([JobId]) REFERENCES [biflow].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [FK_Subscription_User] FOREIGN KEY ([UserId]) REFERENCES [biflow].[User] ([UserId]) ON DELETE CASCADE,
	CONSTRAINT [CK_Subscription_SubscriptionType] CHECK ([SubscriptionType] = 'OnFailure' OR [SubscriptionType] = 'OnSuccess' OR [SubscriptionType] = 'OnCompletion')
)
