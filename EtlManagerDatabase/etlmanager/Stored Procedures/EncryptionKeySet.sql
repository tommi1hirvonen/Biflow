CREATE PROCEDURE [etlmanager].[EncryptionKeySet]
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

TRUNCATE TABLE etlmanager.EncryptionKey

INSERT INTO etlmanager.EncryptionKey (EncryptionKey, Entropy)
SELECT @NewEncryptionKeyEncrypted, @Entropy


COMMIT TRANSACTION