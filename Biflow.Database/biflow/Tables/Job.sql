CREATE TABLE [biflow].[Job] (
    [JobId]                             UNIQUEIDENTIFIER NOT NULL,
    [JobName]                           NVARCHAR(250)    NOT NULL,
    [JobDescription]                    NVARCHAR(MAX)    NULL,
    [CreatedDateTime]                   DATETIMEOFFSET   NOT NULL,
    [LastModifiedDateTime]              DATETIMEOFFSET   NOT NULL,
    [UseDependencyMode]                 BIT              NOT NULL,
    [StopOnFirstError]                  BIT              CONSTRAINT [DF_Job_StopOnFirstError] DEFAULT (0) NOT NULL,
    [MaxParallelSteps]                  INT              CONSTRAINT [DF_Job_MaxParallelSteps] DEFAULT (0) NOT NULL ,
    [OvertimeNotificationLimitMinutes]  FLOAT            CONSTRAINT [DF_Job_OvertimeNotificationLimitMinutes] DEFAULT(0) NOT NULL,
    [IsEnabled]                         BIT              CONSTRAINT [DF_Job_IsEnabled] DEFAULT (1) NOT NULL,
    [CreatedBy]                         NVARCHAR(250)    NULL,
    [LastModifiedBy]                    NVARCHAR(250)    NULL,
    [Timestamp]                         ROWVERSION       NOT NULL,
    [JobCategoryId] UNIQUEIDENTIFIER CONSTRAINT [FK_Job_Category] FOREIGN KEY REFERENCES [biflow].[JobCategory] ([JobCategoryId]) ON DELETE SET NULL,
    CONSTRAINT [PK_Job] PRIMARY KEY CLUSTERED ([JobId] ASC)
);


GO

CREATE TRIGGER [biflow].[Trigger_Job]
    ON [biflow].[Job]
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON
        -- Use instead of trigger to delete linking dependencies because of SQL Server limitation with multiple cascading paths.
        -- https://support.microsoft.com/en-us/help/321843/error-message-1785-occurs-when-you-create-a-foreign-key-constraint-tha
        DELETE FROM biflow.Step WHERE JobId IN (SELECT JobId FROM deleted) OR JobToExecuteId IN (SELECT JobId FROM deleted)
        DELETE FROM biflow.JobParameter WHERE JobId IN (SELECT JobId FROM deleted)
        DELETE FROM biflow.Job WHERE JobId IN (SELECT JobId FROM deleted)
        
    END