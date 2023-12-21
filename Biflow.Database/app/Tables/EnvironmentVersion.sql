CREATE TABLE [app].[EnvironmentVersion]
(
	[VersionId]			INT IDENTITY(1,1)	NOT NULL,
	[Description]		NVARCHAR(MAX)		NULL,
	[Snapshot]			NVARCHAR(MAX)		NOT NULL,
	[CreatedDateTime]	DATETIMEOFFSET		NOT NULL,
	[CreatedBy]			NVARCHAR(250)		NULL,
	CONSTRAINT [PK_EnvironmentVersion] PRIMARY KEY CLUSTERED ([VersionId])
)
