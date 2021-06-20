CREATE TABLE [etlmanager].[Schedule] (
    [ScheduleId]        UNIQUEIDENTIFIER    NOT NULL,
    [JobId]             UNIQUEIDENTIFIER    NOT NULL,
    [CronExpression]    VARCHAR(200)        NOT NULL,
    [IsEnabled]         BIT                 CONSTRAINT [DF_Schedule_IsEnabled] DEFAULT ((1)) NOT NULL,
    [CreatedDateTime]   DATETIME2           NOT NULL,
    [CreatedBy]         NVARCHAR(250)       NULL,
    CONSTRAINT [PK_Schedule] PRIMARY KEY CLUSTERED ([ScheduleId] ASC),
    CONSTRAINT [FK_Schedule_Job] FOREIGN KEY ([JobId]) REFERENCES [etlmanager].[Job] ([JobId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Schedule] UNIQUE ([JobId], [CronExpression])
);


GO

CREATE TRIGGER [etlmanager].[Trigger_Schedule]
    ON [etlmanager].[Schedule]
    INSTEAD OF INSERT
    AS
    BEGIN
        
        INSERT INTO etlmanager.Schedule (
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
            FROM etlmanager.Schedule AS X
            WHERE A.JobId = X.JobId AND A.CronExpression = X.CronExpression
        )

    END