namespace Biflow.Core.Entities;

public class PipelineFolder(string? name, PipelineFolder? parent)
{
    private readonly List<PipelineInfo> _pipelines = [];
    private readonly List<PipelineFolder> _folders = [];
    private readonly PipelineFolder? _parent = parent;

    public string? Name { get; } = name;

    public int Depth => _parent is null ? 0 : _parent.Depth + 1;

    public IEnumerable<PipelineInfo> Pipelines => _pipelines;

    public IEnumerable<PipelineFolder> Folders => _folders;

    public static PipelineFolder FromPipelines(IEnumerable<PipelineInfo> pipelines)
    {
        var root = new PipelineFolder(null, null);

        foreach (var pipeline in pipelines)
        {
            AddFileToFolder(root, pipeline);
        }

        return root;
    }

    private static void AddFileToFolder(PipelineFolder folder, PipelineInfo pipeline)
    {
        if (pipeline.Folder is null)
        {
            folder._pipelines.Add(pipeline);
            return;
        }

        var pathComponents = pipeline.Folder.Split('/');

        foreach (var component in pathComponents)
        {
            var subfolder = folder._folders.FirstOrDefault(x => x.Name == component);
            if (subfolder is null)
            {
                subfolder = new PipelineFolder(component, folder);
                folder._folders.Add(subfolder);
            }

            folder = subfolder;
        }

        folder._pipelines.Add(pipeline);
    }
}
