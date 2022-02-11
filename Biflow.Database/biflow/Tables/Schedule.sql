CREATE TABLE [biflow].[Schedule] (
    [ScheduleId]        UNIQUEIDENTIFIER    NOT NULL,
    [JobId]             UNIQUEIDENTIFIER    NOT NULL,
    [CronExpression]    VARCHAR(200)        NOT NULL,
    [IsEnabled]         BIT                 CONSTRAINT [DF_Schedule_IsEnabled] DEFAULT ((1)) NOT NULL,
    [CreatedDateTime]   DATETIMEOFFSET      NOT NULL,
    [CreatedBy]         NVARCHAR(250)       NULL,
    CONSTRAINT [PK_Schedule] PRIMARY KEY CLUSTERED ([ScheduleId] ASC),
    CONSTRAINT [FK_Schedule_Job] FOREIGN KEY ([JobId]) REFERENCES [biflow].[Job] ([JobId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Schedule] UNIQUE ([JobId], [CronExpression])
);


GO

CREATE TRIGGER [biflow].[Trigger_Schedule]
    ON [biflow].[Schedule]
    INSTEAD OF INSERT
    AS
    BEGIN
        
        INSERT INTO biflow.Schedule (
            ScheduleId,
            JobId,
            CronExpression,
            IsEnabled,
            CreatedDateTime,
            CreatedBy
        )
        SELECT
            ScheduleId,
            JobId,
            CronExpression,
            IsEnabled,
            CreatedDateTime,
            CreatedBy
        FROM inserted AS A
        WHERE NOT EXISTS (
            SELECT *
            FROM biflow.Schedule AS X
            WHERE A.JobId = X.JobId AND A.CronExpression = X.CronExpression
        )

    END