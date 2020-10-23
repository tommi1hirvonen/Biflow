

CREATE PROCEDURE [etlmanager].[UserUpdatePassword]
	@Username [nvarchar](250),
	@Password [nvarchar](250)
AS
BEGIN

SET NOCOUNT ON;

IF ISNULL(@Username, '') IS NULL OR ISNULL(@Password, '') IS NULL
BEGIN

	SELECT 0;

	RETURN;

END;

BEGIN TRY


	UPDATE [etlmanager].[User]
	SET [PasswordHash] = HASHBYTES('SHA2_512', @Password + CONVERT([nvarchar](36), [Salt])),
		[LastModifiedDateTime] = GETDATE()
	WHERE [Username] = @Username
	;

	SELECT 1;

END TRY
BEGIN CATCH

	SELECT 0;

END CATCH;


END;
