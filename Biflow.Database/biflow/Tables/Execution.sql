CREATE TABLE [biflow].[Execution] (
    [ExecutionId]                       UNIQUEIDENTIFIER    NOT NULL,
    [JobId]                             UNIQUEIDENTIFIER    NOT NULL,
    [JobName]                           NVARCHAR(250)       NOT NULL,
    [CreatedDateTime]                   DATETIMEOFFSET      NOT NULL,
    [StartDateTime]                     DATETIMEOFFSET      NULL,
    [EndDateTime]                       DATETIMEOFFSET      NULL,
    [ExecutionStatus]                   VARCHAR(50)         NOT NULL,
    [DependencyMode]                    BIT                 NOT NULL,
    [StopOnFirstError]                  BIT                 CONSTRAINT [DF_Execution_StopOnFirstError] DEFAULT (0) NOT NULL,
    [MaxParallelSteps]                  INT                 NOT NULL CONSTRAINT [DF_Execution_MaxParallelSteps] DEFAULT (0),
    [OvertimeNotificationLimitMinutes]  FLOAT               NOT NULL CONSTRAINT [DF_Execution_OvertimeNotificationLimitMinutes] DEFAULT(0),
    [CreatedBy]                         NVARCHAR(250)       NULL,
    [ScheduleId]                        UNIQUEIDENTIFIER    NULL,
    [ExecutorProcessId]                 INT                 NULL,
    [Notify]                            BIT                 NOT NULL CONSTRAINT [DF_Execution_Notify] DEFAULT (0),
    [NotifyCaller]                      VARCHAR(20)         NULL,
    [NotifyCallerOvertime]              BIT                 NOT NULL CONSTRAINT [DF_Execution_NotifyCallerOvertime] DEFAULT (0),
    [ScheduleName]                      NVARCHAR(250)       NULL,
    [CronExpression]                    VARCHAR(200)        NULL,
    [ParentExecution]                   VARCHAR(200)        NULL,
    CONSTRAINT [PK_Execution] PRIMARY KEY CLUSTERED ([ExecutionId] ASC),
     -- Index used by Jobs page to show the last execution for each job
    INDEX [NCI_Execution_JobId_CreatedDateTime] NONCLUSTERED ([JobId], [CreatedDateTime] DESC),
    -- Index used by Executions page to filter executions based on their execution time
    INDEX [NCI_Execution_CreatedDateTime_EndDateTime] NONCLUSTERED ([CreatedDateTime], [EndDateTime]),
    -- Index used by Executions page to add running executions to the list of reported executions.
    INDEX [NCI_Execution_ExecutionStatus] NONCLUSTERED ([ExecutionStatus]),
    CONSTRAINT [CK_Execution_ExecutionStatus] CHECK (
        [ExecutionStatus] = 'NotStarted'
        OR [ExecutionStatus] = 'Running'
        OR [ExecutionStatus] = 'Succeeded'
        OR [ExecutionStatus] = 'Failed'
        OR [ExecutionStatus] = 'Warning'
        OR [ExecutionStatus] = 'Stopped'
        OR [ExecutionStatus] = 'Suspended'),
    CONSTRAINT [CK_Execution_NotifyCaller] CHECK (
        [NotifyCaller] = 'OnFailure'
        OR [NotifyCaller] = 'OnSuccess'
        OR [NotifyCaller] = 'OnCompletion')
);

GO

CREATE TRIGGER [biflow].[Trigger_Execution]
    ON [biflow].[Execution]
    INSTEAD OF DELETE
    AS
    BEGIN
        SET NOCOUNT ON

        DELETE FROM biflow.ExecutionStep
        WHERE ExecutionId IN (SELECT ExecutionId FROM deleted)

        DELETE FROM biflow.ExecutionParameter
        WHERE ExecutionId IN (SELECT ExecutionId FROM deleted)

        DELETE FROM biflow.Execution
        WHERE ExecutionId IN (SELECT ExecutionId FROM deleted)

    END