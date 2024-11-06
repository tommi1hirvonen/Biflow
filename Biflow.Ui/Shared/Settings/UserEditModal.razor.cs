using Biflow.Ui.Core.Validation;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Data;

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

    private List<Job>? jobs;
    private List<MasterDataTable>? dataTables;
    private HxModal? modal;
    private UserFormModel? model;
    private Guid previousUserId;
    private string currentUsername = "";
    private UserFormModelValidator validator = new([]);
    private AuthorizationPane currentPane = AuthorizationPane.Jobs;

    private bool IsNewUser => previousUserId == Guid.Empty;

    private void ToggleJobAuthorization(ChangeEventArgs args, Job job)
    {
        ArgumentNullException.ThrowIfNull(model);
        var enabled = (bool)args.Value!;
        if (enabled)
        {
            model.User.Jobs.Add(job);
        }
        else
        {
            model.User.Jobs.Remove(job);
        }
    }

    private void ToggleDataTableAuthorization(ChangeEventArgs args, MasterDataTable table)
    {
        ArgumentNullException.ThrowIfNull(model);
        var enabled = (bool)args.Value!;
        if (enabled)
        {
            model.User.DataTables.Add(table);
        }
        else
        {
            model.User.DataTables.Remove(table);
        }
    }

    private void OnClosed()
    {
        model = null;
    }

    private async Task SubmitUser()
    {
        ArgumentNullException.ThrowIfNull(model);
        // New user
        if (IsNewUser)
        {
            ArgumentNullException.ThrowIfNull(model.PasswordModel);
            if (_authenticationResolver.AuthenticationMethod == AuthenticationMethod.BuiltIn && !model.PasswordModel.Password.Equals(model.PasswordModel.ConfirmPassword))
            {
                _toaster.AddError("The two passwords do not match");
                return;
            }

            try
            {
                await _mediator.SendAsync(new CreateUserCommand(model.User, model.PasswordModel));
                await OnUserSubmit.InvokeAsync(model.User);
                model = null;
                await modal.LetAsync(x => x.HideAsync());
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
                ArgumentNullException.ThrowIfNull(model);
                await _mediator.SendAsync(new UpdateUserCommand(model.User));
                await OnUserSubmit.InvokeAsync(model.User);
                await modal.LetAsync(x => x.HideAsync());
            }
            catch (Exception ex)
            {
                _toaster.AddError("Error updating user", ex.Message);
            }
        }
    }

    public async Task ShowAsync(Guid? userId)
    {
        currentUsername = "";
        await modal.LetAsync(x => x.ShowAsync());
        previousUserId = userId ?? Guid.Empty;
        using var context = _dbContextFactory.CreateDbContext();
        if (userId is null)
        {
            var user = new User
            {
                Username = ""
            };
            model = new(user, new());
        }
        else
        {
            var user = await context.Users
                .Include(u => u.Jobs)
                .Include(u => u.DataTables)
                .FirstAsync(user => user.UserId == userId);
            currentUsername = user.Username;
            model = new(user, null); // no password model for existing users
        }
        jobs = await context.Jobs
            .OrderBy(j => j.JobName)
            .ToListAsync();
        dataTables = await context.MasterDataTables
            .Include(t => t.Category)
            .OrderBy(t => t.DataTableName)
            .ToListAsync();
        var reservedUsernames = await context.Users
            .AsNoTracking()
            .Where(u => u.Username != model.User.Username)
            .Select(u => u.Username)
            .ToArrayAsync();
        validator = new(reservedUsernames);
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
