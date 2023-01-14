namespace Biflow.Ui.Shared.StepEdit;

public record PackageSelectedResponse(string Folder, string Project, string Package)
{
    public void Deconstruct(out string folder, out string project, out string package)
    {
        (folder, project, package) = (Folder , Project, Package);
    }
}
