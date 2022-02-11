
CREATE PROCEDURE [biflow].[UserAdd]
	@Username [nvarchar](250),
	@Password [nvarchar](250),
	@Role	  [varchar](50),
	@Email	  [nvarchar](250) = NULL
AS
BEGIN

SET NOCOUNT ON;

IF ISNULL(@Username, '') IS NULL OR ISNULL(@Password, '') IS NULL OR ISNULL(@Role, '') IS NULL
BEGIN

	SELECT 0;

	RETURN;

END;


DECLARE @Salt [uniqueidentifier] = NEWID();

BEGIN TRY

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
		HASHBYTES('SHA2_512', @Password + CONVERT([nvarchar](36), @Salt)),
		@Salt,
		@Role,
		@Email,
		GETUTCDATE(),
		GETUTCDATE()
	;

	SELECT 1;

END TRY
BEGIN CATCH

	SELECT 0;

END CATCH;


END;