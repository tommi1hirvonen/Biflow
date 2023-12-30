namespace Biflow.Ui.SqlServer;

public class SSISCatalog(Dictionary<long, CatalogFolder> folders)
{
    public Dictionary<long, CatalogFolder> Folders { get; } = folders;
}
