﻿CREATE TABLE [app].[JobAuthorization]
(
	[JobId] UNIQUEIDENTIFIER NOT NULL,
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_JobAuthorization] PRIMARY KEY CLUSTERED ([JobId], [UserId]),
	CONSTRAINT [FK_JobAuthorization_Job] FOREIGN KEY ([JobId]) REFERENCES [app].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [FK_JobAuthorization_User] FOREIGN KEY ([UserId]) REFERENCES [app].[User] ([UserId]) ON DELETE CASCADE
)
