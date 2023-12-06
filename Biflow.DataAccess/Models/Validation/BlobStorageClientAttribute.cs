using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models.Validation;

internal class BlobStorageClientAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        (validationContext.ObjectInstance) switch
        {
            BlobStorageClient client and { ConnectionMethod: BlobStorageConnectionMethod.Url } => IsUrlClientValid(client),
            BlobStorageClient client and { ConnectionMethod: BlobStorageConnectionMethod.ConnectionString } => IsConnectionStringClientValid(client),
            BlobStorageClient client and { ConnectionMethod: BlobStorageConnectionMethod.AppRegistration } => IsAppRegistrationClientValid(client),
            BlobStorageClient client => new ValidationResult($"Unrecognized {nameof(client.ConnectionMethod)} value {client.ConnectionMethod}"),
            _ => new ValidationResult($"Object is not of type {nameof(BlobStorageClient)}")
        };

    private static ValidationResult? IsUrlClientValid(BlobStorageClient client) =>
        client.StorageAccountUrl is not null
        ? ValidationResult.Success
        : new ValidationResult("Storace account URL is required");

    private static ValidationResult? IsConnectionStringClientValid(BlobStorageClient client) =>
        client.ConnectionString is not null
        ? ValidationResult.Success
        : new ValidationResult("Connection string is required");

    private static ValidationResult? IsAppRegistrationClientValid(BlobStorageClient client) =>
        client.AppRegistrationId is not null && client.StorageAccountUrl is not null
        ? ValidationResult.Success
        : new ValidationResult("App registration and storage account URL are required");
}
