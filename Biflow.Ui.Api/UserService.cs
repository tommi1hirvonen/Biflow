using Biflow.DataAccess;
using JetBrains.Annotations;

namespace Biflow.Ui.Api;

[UsedImplicitly]
internal class UserService : IUserService
{
    public string? Username { get; internal set; }
    
    public IEnumerable<string>? Roles => null; // null => disables query filters in AppDbContext
}