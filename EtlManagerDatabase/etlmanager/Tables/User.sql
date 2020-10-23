CREATE TABLE [etlmanager].[User] (
    [Username]             NVARCHAR (250)   NOT NULL,
    [PasswordHash]         BINARY (64)      NOT NULL,
    [Salt]                 UNIQUEIDENTIFIER NOT NULL,
    [Email]                VARCHAR(254)     NULL,
    [Role]                 VARCHAR(50)      NOT NULL,
    [CreatedDateTime]      DATETIME2 (7)    NOT NULL,
    [LastModifiedDateTime] DATETIME2 (7)    NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Username] ASC),
    CONSTRAINT [CK_User] CHECK ([Role] = 'Admin' OR [Role] = 'Editor' OR [Role] = 'Operator' OR [Role] = 'Viewer')
);


GO

CREATE TRIGGER [etlmanager].[Trigger_User]
    ON [etlmanager].[User]
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON
        DELETE FROM [etlmanager].[Subscription] WHERE [Username] IN (SELECT [Username] FROM [deleted])
        DELETE FROM [etlmanager].[User] WHERE [Username] IN (SELECT [Username] FROM [deleted])
    END