

CREATE PROCEDURE [biflow].[UserUpdatePassword]
	@Username [nvarchar](250),
	@Password [nvarchar](250)
AS
BEGIN

SET XACT_ABORT ON
SET NOCOUNT ON

IF NULLIF(@Username, '') IS NULL
	THROW 50000, 'Username cannot be null or empty', 1

IF NULLIF(@Password, '') IS NULL
	THROW 50000, 'Password cannot be null or empty', 1

DECLARE @temp TABLE (Username [nvarchar](250) NOT NULL)

DECLARE @Salt [uniqueidentifier] = NEWID()

UPDATE [biflow].[User]
SET [PasswordHash] = HASHBYTES('SHA2_512', @Password + CONVERT([nvarchar](36), @Salt)),
	[Salt] = @Salt,
	[LastModifiedDateTime] = GETUTCDATE()
OUTPUT inserted.Username INTO @temp
WHERE [Username] = @Username

SELECT COUNT(*) FROM @temp

END