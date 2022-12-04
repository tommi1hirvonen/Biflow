namespace Biflow.Ui.Core;

public class AuthenticationMethodResolver
{
    public AuthenticationMethod AuthenticationMethod { get; }

    internal AuthenticationMethodResolver(AuthenticationMethod authenticationMethod)
    {
        AuthenticationMethod = authenticationMethod;
    }
}
