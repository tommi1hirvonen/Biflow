namespace Biflow.DataAccess;

/// <summary>
/// Contract to provide access to the current user in services.
/// </summary>
public interface IUserService
{
    public string? Username { get; }
    
    public IEnumerable<string>? Roles { get; }
}
