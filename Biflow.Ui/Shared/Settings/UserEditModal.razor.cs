using Biflow.Ui.Core.Validation;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Data;

namespace Biflow.Ui.Shared.Settings;

public partial class UserEditModal : ComponentBase
{
    [Inject] private AuthenticationMethodResolver AuthenticationResolver { get; set; } = null!;
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public EventCallback<User> OnUserSubmit { get; set; }

    private List<Job>? jobs;
    private List<MasterDataTable>? dataTables;
    private HxModal? modal;
    private UserFormModel? model;
    private Guid previousUserId;
    private string currentUsername = "";
    private UserFormModelValidator validator = new(Enumerable.Empty<string>());
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
            if (AuthenticationResolver.AuthenticationMethod == AuthenticationMethod.BuiltIn && !model.PasswordModel.Password.Equals(model.PasswordModel.ConfirmPassword))
            {
                Messenger.AddError("The two passwords do not match");
                return;
            }

            try
            {
                await Mediator.SendAsync(new CreateUserCommand(model.User, model.PasswordModel));
                await OnUserSubmit.InvokeAsync(model.User);
                model = null;
                await modal.LetAsync(x => x.HideAsync());
            }
            catch (Exception ex)
            {
                Messenger.AddError("Error creating user", ex.Message);
            }
        }
        // Existing user
        else
        {
            try
            {
                ArgumentNullException.ThrowIfNull(model);
                await Mediator.SendAsync(new UpdateUserCommand(model.User));
                await OnUserSubmit.InvokeAsync(model.User);
                await modal.LetAsync(x => x.HideAsync());
            }
            catch (Exception ex)
            {
                Messenger.AddError("Error updating user", ex.Message);
            }
        }
    }

    public async Task ShowAsync(Guid? userId)
    {
        currentUsername = "";
        await modal.LetAsync(x => x.ShowAsync());
        previousUserId = userId ?? Guid.Empty;
        using var context = DbFactory.CreateDbContext();
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
            .Include(j => j.Category)
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
        var confirmed = await JS.InvokeAsync<bool>("confirm", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }

    private enum AuthorizationPane { Jobs, DataTables }
}
