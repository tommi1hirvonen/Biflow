CREATE TABLE [etlmanager].[FunctionApp]
(
	[FunctionAppId] UNIQUEIDENTIFIER NOT NULL,
	[FunctionAppName] NVARCHAR(250) NOT NULL,
	[FunctionAppUrl] VARCHAR(1000) NULL,
	[FunctionAppKey] VARBINARY(MAX) NULL,
	CONSTRAINT [PK_FunctionApp] PRIMARY KEY CLUSTERED ([FunctionAppId] ASC)
)
