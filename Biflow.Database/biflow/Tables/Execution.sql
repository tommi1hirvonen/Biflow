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
    [OvertimeNotificationLimitMinutes]  INT                 NOT NULL CONSTRAINT [DF_Execution_OvertimeNotificationLimitMinutes] DEFAULT(0),
    [CreatedBy]                         NVARCHAR(250)       NULL,
    [ScheduleId]                        UNIQUEIDENTIFIER    NULL,
    [ExecutorProcessId]                 INT                 NULL,
    [Notify]                            BIT                 NOT NULL CONSTRAINT [DF_Execution_Notify] DEFAULT (0),
    [NotifyCaller]                      VARCHAR(20)         NULL,
    [NotifyCallerOvertime]              BIT                 NOT NULL CONSTRAINT [DF_Execution_NotifyCallerOvertime] DEFAULT (0),
    CONSTRAINT [PK_Execution] PRIMARY KEY CLUSTERED ([ExecutionId] ASC),
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

