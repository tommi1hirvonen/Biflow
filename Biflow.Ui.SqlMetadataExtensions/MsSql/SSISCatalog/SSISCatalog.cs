namespace Biflow.Ui.SqlMetadataExtensions;

public class SSISCatalog(Dictionary<long, CatalogFolder> folders)
{
    public Dictionary<long, CatalogFolder> Folders { get; } = folders;
}
