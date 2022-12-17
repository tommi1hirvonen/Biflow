CREATE TABLE [biflow].[JobCategory]
(
	[JobCategoryId] UNIQUEIDENTIFIER NOT NULL,
	[JobCategoryName] NVARCHAR(250) NOT NULL,
	CONSTRAINT [PK_JobCategory] PRIMARY KEY CLUSTERED ([JobCategoryId]),
	CONSTRAINT [UQ_JobCategory] UNIQUE ([JobCategoryName])
)
