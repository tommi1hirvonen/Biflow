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
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Biflow.Ui.Shared.Settings;

public partial class UserEditModal : ComponentBase, IDisposable
{
    [Inject] private AuthenticationMethodResolver AuthenticationResolver { get; set; } = null!;
    
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
            
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public EventCallback<User> OnUserSubmit { get; set; }

    private List<Job>? Jobs { get; set; }
    private List<MasterDataTable>? DataTables { get; set; }

    private HxModal? Modal { get; set; }

    private UserFormModel? Model { get; set; }

    private bool IsNewUser => PreviousUserId == Guid.Empty;
    private Guid PreviousUserId { get; set; }
    private string CurrentUsername { get; set; } = "";

    private AppDbContext Context { get; set; } = null!;

    private UserFormModelValidator Validator { get; set; } = new(Enumerable.Empty<string>());

    private AuthorizationPane CurrentPane { get; set; } = AuthorizationPane.Jobs;

    private async Task ResetContext()
    {
        if (Context is not null)
            await Context.DisposeAsync();

        Context = await DbFactory.CreateDbContextAsync();
    }

    private void ToggleRole(string role)
    {
        ArgumentNullException.ThrowIfNull(Model);
        if (role == Roles.Admin)
        {
            Model.User.Roles.Clear();
            Model.User.Roles.Add(role);
        }
        else if (role == Roles.Editor || role == Roles.Operator || role == Roles.Viewer)
        {
            Model.User.Roles.RemoveAll(r => r == Roles.Admin || r == Roles.Editor || r == Roles.Viewer || r == Roles.Operator);
            Model.User.Roles.Add(role);
        }
        else if (role == Roles.DataTableMaintainer)
        {
            if (Model.User.Roles.Contains(role))
            {
                Model.User.Roles.RemoveAll(r => r == Roles.DataTableMaintainer);
            }
            else
            {
                Model.User.Roles.Add(role);
            }
        }
        else if (role == Roles.SettingsEditor)
        {
            if (Model.User.Roles.Contains(role))
            {
                Model.User.Roles.RemoveAll(r => r == Roles.SettingsEditor);
            }
            else
            {
                Model.User.Roles.Add(role);
            }
        }
        else
        {
            throw new ArgumentException($"Unrecognized role {role}", nameof(role));
        }
    }

    private void ToggleJobAuthorization(ChangeEventArgs args, Job job)
    {
        ArgumentNullException.ThrowIfNull(Model);
        var enabled = (bool)args.Value!;
        if (enabled)
        {
            Model.User.Jobs.Add(job);
        }
        else
        {
            Model.User.Jobs.Remove(job);
        }
    }

    private void ToggleDataTableAuthorization(ChangeEventArgs args, MasterDataTable table)
    {
        ArgumentNullException.ThrowIfNull(Model);
        var enabled = (bool)args.Value!;
        if (enabled)
        {
            Model.User.DataTables.Add(table);
        }
        else
        {
            Model.User.DataTables.Remove(table);
        }
    }

    private void OnClosed()
    {
        Model = null;
    }

    private async Task SubmitUser()
    {
        ArgumentNullException.ThrowIfNull(Model);
        // New user
        if (IsNewUser)
        {
            ArgumentNullException.ThrowIfNull(Model.PasswordModel);
            if (AuthenticationResolver.AuthenticationMethod == AuthenticationMethod.BuiltIn && !Model.PasswordModel.Password.Equals(Model.PasswordModel.ConfirmPassword))
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
                    context.Users.Add(Model.User);

                    await context.SaveChangesAsync();

                    var connection = context.Database.GetDbConnection();

                    // Update the password hash.
                    await UserService.AdminUpdatePasswordAsync(Model.User.Username, Model.PasswordModel.Password, connection, transaction);

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                await OnUserSubmit.InvokeAsync(Model.User);
                Model = null;
                await Modal.LetAsync(x => x.HideAsync());
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
                ArgumentNullException.ThrowIfNull(Model);
                Context.Attach(Model.User).Property(u => u.Roles).IsModified = true;
                await Context.SaveChangesAsync();
                await OnUserSubmit.InvokeAsync(Model.User);
                await Modal.LetAsync(x => x.HideAsync());
            }
            catch (Exception ex)
            {
                Messenger.AddError("Error updating user", ex.Message);
            }
        }
    }

    public async Task ShowAsync(Guid? userId)
    {
        CurrentUsername = "";
        await Modal.LetAsync(x => x.ShowAsync());
        PreviousUserId = userId ?? Guid.Empty;
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
            Model = new(user, new());
        }
        else
        {
            await ResetContext();
            var user = await Context.Users
                .Include(u => u.Jobs)
                .Include(u => u.DataTables)
                .FirstAsync(user => user.UserId == userId);
            CurrentUsername = user.Username;
            Model = new(user, null); // no password model for existing users
        }
        Jobs = await Context.Jobs
            .Include(j => j.Category)
            .OrderBy(j => j.JobName)
            .ToListAsync();
        DataTables = await Context.MasterDataTables
            .Include(t => t.Category)
            .OrderBy(t => t.DataTableName)
            .ToListAsync();
        var reservedUsernames = await Context.Users
            .AsNoTracking()
            .Where(u => u.Username != Model.User.Username)
            .Select(u => u.Username)
            .ToArrayAsync();
        Validator = new(reservedUsernames);

    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }

    public void Dispose() => Context?.Dispose();

    private enum AuthorizationPane { Jobs, DataTables }
}
