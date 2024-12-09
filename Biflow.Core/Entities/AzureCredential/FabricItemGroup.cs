using Microsoft.Fabric.Api.Core.Models;

namespace Biflow.Core.Entities;

public record FabricItemGroup(Guid WorkspaceId, string WorkspaceName, IEnumerable<Item> Items);