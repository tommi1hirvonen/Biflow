CREATE TABLE [app].[Tag]
(
	[TagId] UNIQUEIDENTIFIER NOT NULL,
	[TagName] NVARCHAR(250) NOT NULL,
	[Color] VARCHAR(20) NOT NULL CONSTRAINT [DF_Tag_Color] DEFAULT ('LightGray'),
	CONSTRAINT [PK_Tag] PRIMARY KEY CLUSTERED ([TagId]),
	CONSTRAINT [UQ_TagName] UNIQUE ([TagName]),
	CONSTRAINT [CK_Tag_Color] CHECK (
	[Color] = 'LightGray' OR
	[Color] = 'DarkGray' OR
	[Color] = 'Purple' OR
	[Color] = 'Green' OR
	[Color] = 'Blue' OR
	[Color] = 'Yellow' OR
	[Color] = 'Red'        
	)
)
