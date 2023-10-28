using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Core.Validation;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.JSInterop;
using System.Data;

namespace Biflow.Ui.Shared.Settings;

public partial class UserEditModal : ComponentBase, IDisposable
{
    [Inject] private AuthenticationMethodResolver AuthenticationResolver { get; set; } = null!;
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public EventCallback<User> OnUserSubmit { get; set; }

    private List<Job>? jobs;
    private List<MasterDataTable>? dataTables;
    private HxModal? modal;
    private UserFormModel? model;
    private Guid previousUserId;
    private string currentUsername = "";
    private AppDbContext context = null!;
    private UserFormModelValidator validator = new(Enumerable.Empty<string>());
    private AuthorizationPane currentPane = AuthorizationPane.Jobs;

    private bool IsNewUser => previousUserId == Guid.Empty;

    private async Task ResetContext()
    {
        if (context is not null)
            await context.DisposeAsync();

        context = await DbFactory.CreateDbContextAsync();
    }

    private void ToggleRole(string role)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (role == Roles.Admin)
        {
            model.User.Roles.Clear();
            model.User.Roles.Add(role);
        }
        else if (role == Roles.Editor || role == Roles.Operator || role == Roles.Viewer)
        {
            model.User.Roles.RemoveAll(r => r == Roles.Admin || r == Roles.Editor || r == Roles.Viewer || r == Roles.Operator);
            model.User.Roles.Add(role);
        }
        else if (role == Roles.DataTableMaintainer)
        {
            if (model.User.Roles.Contains(role))
            {
                model.User.Roles.RemoveAll(r => r == Roles.DataTableMaintainer);
            }
            else
            {
                model.User.Roles.Add(role);
            }
        }
        else if (role == Roles.SettingsEditor)
        {
            if (model.User.Roles.Contains(role))
            {
                model.User.Roles.RemoveAll(r => r == Roles.SettingsEditor);
            }
            else
            {
                model.User.Roles.Add(role);
            }
        }
        else
        {
            throw new ArgumentException($"Unrecognized role {role}", nameof(role));
        }
    }

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
                var context = await DbFactory.CreateDbContextAsync();
                var transaction = context.Database.BeginTransaction().GetDbTransaction();

                try
                {
                    // Add user without password
                    context.Users.Add(model.User);

                    await context.SaveChangesAsync();

                    var connection = context.Database.GetDbConnection();

                    // Update the password hash.
                    await UserService.AdminUpdatePasswordAsync(model.User.Username, model.PasswordModel.Password, connection, transaction);

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

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
                context.Attach(model.User).Property(u => u.Roles).IsModified = true;
                await context.SaveChangesAsync();
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
        if (userId is null)
        {
            await ResetContext();
            var user = new User
            {
                Username = "",
                Roles = [Roles.Viewer],
                Jobs = new List<Job>(),
                DataTables = new List<MasterDataTable>()
            };
            model = new(user, new());
        }
        else
        {
            await ResetContext();
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

    public void Dispose() => context?.Dispose();

    private enum AuthorizationPane { Jobs, DataTables }
}
