CREATE TABLE [biflow].[JobAuthorization]
(
	[JobId] UNIQUEIDENTIFIER NOT NULL,
	[Username] NVARCHAR (250) NOT NULL,
	CONSTRAINT [PK_JobAuthorization] PRIMARY KEY CLUSTERED ([JobId], [Username]),
	CONSTRAINT [FK_JobAuthorization_Job] FOREIGN KEY ([JobId]) REFERENCES [biflow].[Job] ([JobId]) ON DELETE CASCADE,
	CONSTRAINT [FK_JobAuthorization_User] FOREIGN KEY ([Username]) REFERENCES [biflow].[User] ([Username]) ON DELETE CASCADE
)
