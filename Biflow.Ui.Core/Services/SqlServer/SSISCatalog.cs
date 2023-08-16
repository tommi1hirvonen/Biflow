namespace Biflow.Ui.Core;

public class SSISCatalog
{
    public SSISCatalog(Dictionary<long, CatalogFolder> folders)
    {
        Folders = folders;
    }

    public Dictionary<long, CatalogFolder> Folders { get; }
}
