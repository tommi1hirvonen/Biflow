CREATE PROCEDURE [biflow].[UserAuthenticate]
	@Username [nvarchar](250),
	@Password [nvarchar](250)
AS
BEGIN

SET NOCOUNT ON;

DECLARE @Role VARCHAR(50) = (
	SELECT [Role]
	FROM [biflow].[User]
	WHERE [Username] = @Username
		AND [PasswordHash] = HASHBYTES('SHA2_512', @Password + CONVERT([nvarchar](36), [Salt]))
);

SELECT @Role; -- null => authentication failed

END;