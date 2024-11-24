using Biflow.Ui.Core.Validation;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Biflow.Ui.Shared.Settings;

public partial class UserEditModal(
    AuthenticationMethodResolver authenticationResolver,
    IDbContextFactory<AppDbContext> dbContextFactory,
    IMediator mediator,
    ToasterService toaster,
    IJSRuntime js) : ComponentBase
{
    [Parameter] public EventCallback<User> OnUserSubmit { get; set; }

    private readonly AuthenticationMethodResolver _authenticationResolver = authenticationResolver;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly IMediator _mediator = mediator;
    private readonly ToasterService _toaster = toaster;
    private readonly IJSRuntime _js = js;

    private List<Job>? _jobs;
    private List<MasterDataTable>? _dataTables;
    private HxModal? _modal;
    private UserFormModel? _model;
    private Guid _previousUserId;
    private string _currentUsername = "";
    private UserFormModelValidator _validator = new([]);
    private AuthorizationPane _currentPane = AuthorizationPane.Jobs;

    private bool IsNewUser => _previousUserId == Guid.Empty;

    private void ToggleJobAuthorization(ChangeEventArgs args, Job job)
    {
        ArgumentNullException.ThrowIfNull(_model);
        var enabled = (bool)args.Value!;
        if (enabled)
        {
            _model.User.Jobs.Add(job);
        }
        else
        {
            _model.User.Jobs.Remove(job);
        }
    }

    private void ToggleDataTableAuthorization(ChangeEventArgs args, MasterDataTable table)
    {
        ArgumentNullException.ThrowIfNull(_model);
        var enabled = (bool)args.Value!;
        if (enabled)
        {
            _model.User.DataTables.Add(table);
        }
        else
        {
            _model.User.DataTables.Remove(table);
        }
    }

    private void OnClosed()
    {
        _model = null;
    }

    private async Task SubmitUser()
    {
        ArgumentNullException.ThrowIfNull(_model);
        // New user
        if (IsNewUser)
        {
            ArgumentNullException.ThrowIfNull(_model.PasswordModel);
            if (_authenticationResolver.AuthenticationMethod == AuthenticationMethod.BuiltIn && !_model.PasswordModel.Password.Equals(_model.PasswordModel.ConfirmPassword))
            {
                _toaster.AddError("The two passwords do not match");
                return;
            }

            try
            {
                await _mediator.SendAsync(new CreateUserCommand(_model.User, _model.PasswordModel));
                await OnUserSubmit.InvokeAsync(_model.User);
                _model = null;
                await _modal.LetAsync(x => x.HideAsync());
            }
            catch (Exception ex)
            {
                _toaster.AddError("Error creating user", ex.Message);
            }
        }
        // Existing user
        else
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_model);
                await _mediator.SendAsync(new UpdateUserCommand(_model.User));
                await OnUserSubmit.InvokeAsync(_model.User);
                await _modal.LetAsync(x => x.HideAsync());
            }
            catch (Exception ex)
            {
                _toaster.AddError("Error updating user", ex.Message);
            }
        }
    }

    public async Task ShowAsync(Guid? userId)
    {
        _currentUsername = "";
        await _modal.LetAsync(x => x.ShowAsync());
        _previousUserId = userId ?? Guid.Empty;
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        if (userId is null)
        {
            var user = new User
            {
                Username = ""
            };
            _model = new(user, new());
        }
        else
        {
            var user = await context.Users
                .Include(u => u.Jobs)
                .Include(u => u.DataTables)
                .FirstAsync(user => user.UserId == userId);
            _currentUsername = user.Username;
            _model = new(user, null); // no password model for existing users
        }
        _jobs = await context.Jobs
            .OrderBy(j => j.JobName)
            .ToListAsync();
        _dataTables = await context.MasterDataTables
            .Include(t => t.Category)
            .OrderBy(t => t.DataTableName)
            .ToListAsync();
        var reservedUsernames = await context.Users
            .AsNoTracking()
            .Where(u => u.Username != _model.User.Username)
            .Select(u => u.Username)
            .ToArrayAsync();
        _validator = new(reservedUsernames);
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        var confirmed = await _js.InvokeAsync<bool>("confirm", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }

    private enum AuthorizationPane { Jobs, DataTables }
}
