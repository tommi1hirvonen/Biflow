
CREATE PROCEDURE [biflow].[UserAdd]
	@Username [nvarchar](250),
	@Password [nvarchar](250),
	@Role	  [varchar](50),
	@Email	  [nvarchar](250) = NULL
AS
BEGIN

SET XACT_ABORT ON
SET NOCOUNT ON

IF NULLIF(@Username, '') IS NULL
	THROW 50000, 'Username cannot be null or empty', 1

IF NULLIF(@Role, '') IS NULL
	THROW 50000, 'Role cannot be null or empty', 1


DECLARE @Salt [uniqueidentifier] = NEWID()

INSERT INTO [biflow].[User] (
	[Username],
	[PasswordHash],
	[Salt],
	[Role],
	[Email],
	[CreatedDateTime],
	[LastModifiedDateTime]
)
SELECT
	@Username,
	CASE
        WHEN @Password IS NOT NULL THEN
            HASHBYTES('SHA2_512', @Password + CONVERT([nvarchar](36), @Salt))
    END,
	@Salt,
	@Role,
	@Email,
	GETUTCDATE(),
	GETUTCDATE()

END