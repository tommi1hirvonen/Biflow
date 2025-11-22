using Microsoft.Fabric.Api.Core.Models;

namespace Biflow.Ui.Components.Shared.StepEdit;

public record FabricItemSelectedResult(Guid WorkspaceId, string WorkspaceName, Item Item);