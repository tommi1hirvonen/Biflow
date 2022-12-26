namespace Biflow.Ui.Core;

public class AuthenticationResult
{
    public bool AuthenticationSuccessful { get; }
    public string? Role { get; }
    public AuthenticationResult(string? role)
    {
        Role = role;
        switch (role)
        {
            case "Admin":
            case "Editor":
            case "Operator":
            case "Viewer":
                AuthenticationSuccessful = true;
                return;
            default:
                AuthenticationSuccessful = false;
                return;
        }
    }
}
