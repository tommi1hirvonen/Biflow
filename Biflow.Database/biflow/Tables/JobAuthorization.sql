CREATE TABLE [biflow].[JobAuthorization]
(
	[JobId] UNIQUEIDENTIFIER NOT NULL,
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_JobAuthorization] PRIMARY KEY CLUSTERED ([JobId], [UserId]),
	CONSTRAINT [FK_JobAuthorization_Job] FOREIGN KEY ([JobId]) REFERENCES [biflow].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [FK_JobAuthorization_User] FOREIGN KEY ([UserId]) REFERENCES [biflow].[User] ([UserId]) ON DELETE CASCADE
)
