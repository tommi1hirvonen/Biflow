CREATE TABLE [biflow].[Schedule] (
    [ScheduleId]        UNIQUEIDENTIFIER    NOT NULL,
    [JobId]             UNIQUEIDENTIFIER    NOT NULL,
    [ScheduleName]      NVARCHAR(250)       NOT NULL,
    [CronExpression]    VARCHAR(200)        NOT NULL,
    [IsEnabled]         BIT                 CONSTRAINT [DF_Schedule_IsEnabled] DEFAULT ((1)) NOT NULL,
    [CreatedDateTime]   DATETIMEOFFSET      NOT NULL,
    [CreatedBy]         NVARCHAR(250)       NULL,
    CONSTRAINT [PK_Schedule] PRIMARY KEY CLUSTERED ([ScheduleId] ASC),
    CONSTRAINT [FK_Schedule_Job] FOREIGN KEY ([JobId]) REFERENCES [biflow].[Job] ([JobId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Schedule] UNIQUE ([JobId], [CronExpression])
)