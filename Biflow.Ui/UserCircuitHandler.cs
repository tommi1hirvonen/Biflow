using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Biflow.Ui;

/// <summary>
/// Captures the current user from <see cref="AuthenticationStateProvider"/>
/// and sets the user in <see cref="IUserService"/> for current user access in scoped services.
/// This type is registered as a scoped service in DI.
/// </summary>
/// <param name="authenticationStateProvider"></param>
/// <param name="userService"></param>
internal sealed class UserCircuitHandler(
    AuthenticationStateProvider authenticationStateProvider,
    IUserService userService) : CircuitHandler, IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider = authenticationStateProvider;
    private readonly IUserService _userService = userService;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _authenticationStateProvider.AuthenticationStateChanged += AuthenticationChanged;
        return base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    private void AuthenticationChanged(Task<AuthenticationState> task)
    {
        _ = UpdateAuthentication(task);
        return;

        async Task UpdateAuthentication(Task<AuthenticationState> task2)
        {
            try
            {
                var state = await task2;
                _userService.SetUser(state.User);
            }
            catch
            {
                // ignored
            }
        }
    }

    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        _userService.SetUser(state.User);
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= AuthenticationChanged;
    }
}