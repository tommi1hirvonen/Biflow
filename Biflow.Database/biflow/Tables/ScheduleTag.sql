CREATE TABLE [biflow].[ScheduleTag]
(
	[ScheduleId] UNIQUEIDENTIFIER NOT NULL,
	[TagId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_ScheduleTag] PRIMARY KEY CLUSTERED ([ScheduleId], [TagId]),
	CONSTRAINT [FK_ScheduleTag_Step] FOREIGN KEY ([ScheduleId]) REFERENCES biflow.Schedule ([ScheduleId]) ON DELETE CASCADE,
	CONSTRAINT [FK_ScheduleTag_Tag] FOREIGN KEY ([TagId]) REFERENCES biflow.Tag ([TagId]) ON DELETE CASCADE
)
