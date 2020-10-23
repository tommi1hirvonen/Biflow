CREATE TABLE [etlmanager].[Schedule] (
    [ScheduleId]        UNIQUEIDENTIFIER    NOT NULL,
    [JobId]             UNIQUEIDENTIFIER    NOT NULL,
    [Monday]            BIT                 CONSTRAINT [DF_Schedule_Monday] DEFAULT ((0)) NOT NULL,
    [Tuesday]           BIT                 CONSTRAINT [DF_Schedule_Tuesday] DEFAULT ((0)) NOT NULL,
    [Wednesday]         BIT                 CONSTRAINT [DF_Schedule_Wednesday] DEFAULT ((0)) NOT NULL,
    [Thursday]          BIT                 CONSTRAINT [DF_Schedule_Thursday] DEFAULT ((0)) NOT NULL,
    [Friday]            BIT                 CONSTRAINT [DF_Schedule_Friday] DEFAULT ((0)) NOT NULL,
    [Saturday]          BIT                 CONSTRAINT [DF_Schedule_Saturday] DEFAULT ((0)) NOT NULL,
    [Sunday]            BIT                 CONSTRAINT [DF_Schedule_Sunday] DEFAULT ((0)) NOT NULL,
    [TimeHours]         INT                 NOT NULL,
    [TimeMinutes]       INT                 NOT NULL,
    [IsEnabled]         BIT                 CONSTRAINT [DF_Schedule_IsEnabled] DEFAULT ((1)) NOT NULL,
    [CreatedDateTime]   DATETIME2           NOT NULL,
    [CreatedBy]         NVARCHAR(250)       NULL,
    CONSTRAINT [PK_Schedule] PRIMARY KEY CLUSTERED ([ScheduleId] ASC),
    CONSTRAINT [CK_Schedule_TimeMinutes] CHECK ([TimeMinutes]=(45) OR [TimeMinutes]=(30) OR [TimeMinutes]=(15) OR [TimeMinutes]=(0)),
    CONSTRAINT [CK_Schedule_TimeHours] CHECK ([TimeHours]>=(0) AND [TimeHours]<=(23)),
    CONSTRAINT [FK_Schedule_Job] FOREIGN KEY ([JobId]) REFERENCES [etlmanager].[Job] ([JobId]),
    CONSTRAINT [UQ_Schedule] UNIQUE ([JobId], [Monday], [Tuesday], [Wednesday], [Thursday], [Friday], [Saturday], [Sunday], [TimeHours], [TimeMinutes])
);

