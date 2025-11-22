using Biflow.ExecutorProxy.Core.FilesExplorer;

namespace Biflow.Ui.Components.Shared.StepEdit;

public delegate Task<IReadOnlyList<DirectoryItem>> FileExplorerDelegate(string? path,
    CancellationToken cancellationToken = default);