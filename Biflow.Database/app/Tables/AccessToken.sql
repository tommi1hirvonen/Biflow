CREATE TABLE [app].[AccessToken]
(
	[AppRegistrationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_AccessToken_AppRegistration] FOREIGN KEY REFERENCES [app].[AppRegistration] ([AppRegistrationId]) ON DELETE CASCADE,
	[ResourceUrl] VARCHAR(1000) NOT NULL,
	[Token] NVARCHAR(MAX) NOT NULL,
	[ExpiresOn] DATETIMEOFFSET NOT NULL,
	CONSTRAINT [PK_AccessToken] PRIMARY KEY CLUSTERED ([AppRegistrationId], [ResourceUrl])
)
