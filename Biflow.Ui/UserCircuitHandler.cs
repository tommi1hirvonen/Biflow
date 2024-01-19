using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Biflow.Ui;

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
        async Task UpdateAuthentication(Task<AuthenticationState> task)
        {
            try
            {
                var state = await task;
                _userService.SetUser(state.User);
            }
            catch
            {
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