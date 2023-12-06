using Azure.Storage.Blobs;

namespace Biflow.DataAccess.Models;

public enum BlobStorageConnectionMethod
{
    AppRegistration, ConnectionString, Url
}
