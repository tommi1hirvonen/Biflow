CREATE TABLE [etlmanager].[Job] (
    [JobId]                UNIQUEIDENTIFIER NOT NULL,
    [JobName]              NVARCHAR(250)    NOT NULL,
    [CreatedDateTime]      DATETIME2 (7)    NOT NULL,
    [LastModifiedDateTime] DATETIME2 (7)    NOT NULL,
    [UseDependencyMode]    BIT              NOT NULL,
    [CreatedBy]            NVARCHAR(250)    NULL,
    [LastModifiedBy]       NVARCHAR(250)    NULL,
    CONSTRAINT [PK_Job] PRIMARY KEY CLUSTERED ([JobId] ASC)
);


GO

CREATE TRIGGER [etlmanager].[Trigger_Job]
    ON [etlmanager].[Job]
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON
        -- Use instead of trigger to delete linking dependencies because of SQL Server limitation with multiple cascading paths.
        -- https://support.microsoft.com/en-us/help/321843/error-message-1785-occurs-when-you-create-a-foreign-key-constraint-tha
        DELETE FROM etlmanager.Step WHERE JobId IN (SELECT JobId FROM deleted)
        DELETE FROM etlmanager.Schedule WHERE JobId IN (SELECT JobId FROM deleted)
        DELETE FROM etlmanager.Subscription WHERE JobId IN (SELECT JobId FROM deleted)
        DELETE FROM etlmanager.Job WHERE JobId IN (SELECT JobId FROM deleted)
    END