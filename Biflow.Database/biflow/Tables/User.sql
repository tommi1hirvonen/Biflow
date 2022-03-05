CREATE TABLE [biflow].[User] (
    [Username]             NVARCHAR (250)   NOT NULL,
    [PasswordHash]         BINARY (64)      NOT NULL,
    [Salt]                 UNIQUEIDENTIFIER NOT NULL,
    [Email]                VARCHAR(254)     NULL,
    [Role]                 VARCHAR(50)      NOT NULL,
    [AuthorizeAllJobs]     BIT              NOT NULL CONSTRAINT [DF_User_AuthorizeAllJobs] DEFAULT (0),
    [CreatedDateTime]      DATETIMEOFFSET   NOT NULL,
    [LastModifiedDateTime] DATETIMEOFFSET   NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Username] ASC),
    CONSTRAINT [CK_User] CHECK ([Role] = 'Admin' OR [Role] = 'Editor' OR [Role] = 'Operator' OR [Role] = 'Viewer')
)