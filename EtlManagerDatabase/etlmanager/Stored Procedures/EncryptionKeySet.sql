CREATE PROCEDURE [etlmanager].[EncryptionKeySet]
	@EncryptionId NVARCHAR(50),
	@OldEncryptionKey NVARCHAR(MAX) = NULL,
	@NewEncryptionKey NVARCHAR(MAX),
	@NewEncryptionKeyEncrypted VARBINARY(MAX),
	@Entropy VARBINARY(MAX)
AS

SET XACT_ABORT ON

BEGIN TRANSACTION

IF @OldEncryptionKey IS NOT NULL
BEGIN

	UPDATE etlmanager.DataFactory
	SET ClientSecret = ENCRYPTBYPASSPHRASE(@NewEncryptionKey, CONVERT(NVARCHAR(MAX), DECRYPTBYPASSPHRASE(@OldEncryptionKey, ClientSecret)))

	UPDATE etlmanager.Connection
	SET ConnectionStringEncrypted = ENCRYPTBYPASSPHRASE(@NewEncryptionKey, CONVERT(NVARCHAR(MAX), DECRYPTBYPASSPHRASE(@OldEncryptionKey, ConnectionStringEncrypted)))
	WHERE IsSensitive = 1

END

DELETE FROM etlmanager.EncryptionKey WHERE EncryptionId = @EncryptionId

INSERT INTO etlmanager.EncryptionKey (EncryptionId, EncryptionKey, Entropy)
SELECT @EncryptionId, @NewEncryptionKeyEncrypted, @Entropy


COMMIT TRANSACTION