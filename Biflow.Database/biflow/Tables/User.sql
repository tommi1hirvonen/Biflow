CREATE TABLE [biflow].[User] (
    [Username]                  NVARCHAR (250)   NOT NULL,
    [PasswordHash]              VARCHAR (100)    NULL,
    [Email]                     VARCHAR(254)     NULL,
    [Roles]                     VARCHAR(500)     NOT NULL,
    [AuthorizeAllJobs]          BIT              NOT NULL CONSTRAINT [DF_User_AuthorizeAllJobs] DEFAULT (0),
    [AuthorizeAllDataTables]    BIT              NOT NULL CONSTRAINT [DF_User_AuthorizeAllDataTables] DEFAULT (0),
    [CreatedDateTime]           DATETIMEOFFSET   NOT NULL,
    [LastModifiedDateTime]      DATETIMEOFFSET   NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Username] ASC)
)