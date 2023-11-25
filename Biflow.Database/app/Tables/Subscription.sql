CREATE TABLE [app].[Subscription]
(
	[SubscriptionId]	UNIQUEIDENTIFIER	NOT NULL,
	[UserId]			UNIQUEIDENTIFIER    NOT NULL,
	[SubscriptionType]  VARCHAR(20)			NOT NULL,
	[AlertType]			VARCHAR(20)			NULL,
	[JobId]				UNIQUEIDENTIFIER	NULL,
	[NotifyOnOvertime]	BIT					NULL,
	[TagId]				UNIQUEIDENTIFIER	NULL,
	[StepId]			UNIQUEIDENTIFIER	NULL,
	CONSTRAINT [PK_Subscription] PRIMARY KEY CLUSTERED ([SubscriptionId]),
	CONSTRAINT [FK_Subscription_Job] FOREIGN KEY ([JobId]) REFERENCES [app].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [FK_Subscription_User] FOREIGN KEY ([UserId]) REFERENCES [app].[User] ([UserId]) ON DELETE CASCADE,
	CONSTRAINT [FK_Subscription_Tag] FOREIGN KEY ([TagId]) REFERENCES [app].[Tag] ([TagId]) ON DELETE CASCADE,
	CONSTRAINT [FK_Subscription_Step] FOREIGN KEY ([StepId]) REFERENCES [app].[Step] ([StepId]) ON DELETE CASCADE,
	INDEX [IX_UQ_Subscription_JobSubscription] UNIQUE ([UserId], [JobId]) WHERE ([SubscriptionType] = 'Job'),
	INDEX [IX_UQ_Subscription_JobTagSubscription] UNIQUE ([UserId], [JobId], [TagId]) WHERE ([SubscriptionType] = 'JobTag'),
	INDEX [IX_UQ_Subscription_TagSubscription] UNIQUE ([UserId], [TagId]) WHERE ([SubscriptionType] = 'Tag'),
	INDEX [IX_UQ_Subscription_StepSubscription] UNIQUE ([UserId], [StepId]) WHERE ([SubscriptionType] = 'Step')
)
