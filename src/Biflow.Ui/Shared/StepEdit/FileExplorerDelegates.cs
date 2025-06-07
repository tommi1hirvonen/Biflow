using Biflow.ExecutorProxy.Core.FilesExplorer;

namespace Biflow.Ui.Shared.StepEdit;

public delegate Task<IReadOnlyList<DirectoryItem>> FileExplorerDelegate(string? path,
    CancellationToken cancellationToken = default);