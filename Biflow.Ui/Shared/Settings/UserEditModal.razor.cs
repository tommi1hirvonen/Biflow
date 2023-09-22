using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
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
    
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;
            
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public EventCallback<User> OnUserSubmit { get; set; }

    private List<Job>? Jobs { get; set; }
    private List<MasterDataTable>? DataTables { get; set; }

    private HxModal? Modal { get; set; }

    private User? User { get; set; }

    private bool IsNewUser => PreviousUsername is null;
    private string? PreviousUsername { get; set; }

    private string? Password { get; set; }
    private string? ConfirmPassword { get; set; }

    private BiflowContext Context { get; set; } = null!;

    private AuthorizationPane CurrentPane { get; set; } = AuthorizationPane.Jobs;

    private async Task ResetContext()
    {
        if (Context is not null)
            await Context.DisposeAsync();

        Context = await DbFactory.CreateDbContextAsync();
    }

    private void ToggleRole(string role)
    {
        ArgumentNullException.ThrowIfNull(User);
        if (role == Roles.Admin || role == Roles.Editor)
        {
            User.Roles.Clear();
            User.Roles.Add(role);
        }
        else if (role == Roles.Operator || role == Roles.Viewer)
        {
            User.Roles.RemoveAll(r => r == Roles.Admin || r == Roles.Editor || r == Roles.Viewer || r == Roles.Operator);
            User.Roles.Add(role);
        }
        else if (role == Roles.DataTableMaintainer || role == Roles.DataTableEditor)
        {
            if (User.Roles.Contains(role))
            {
                User.Roles.RemoveAll(r => r == Roles.DataTableMaintainer || r == Roles.DataTableEditor);
            }
            else
            {
                User.Roles.RemoveAll(r => r == Roles.DataTableMaintainer || r == Roles.DataTableEditor);
                User.Roles.Add(role);
            }
        }
        else
        {
            throw new ArgumentException($"Unrecognized role {role}", nameof(role));
        }
    }

    private void ToggleJobAuthorization(ChangeEventArgs args, Job job)
    {
        var enabled = (bool)args.Value!;
        if (enabled)
        {
            User?.Jobs.Add(job);
        }
        else
        {
            User?.Jobs.Remove(job);
        }
    }

    private void ToggleDataTableAuthorization(ChangeEventArgs args, MasterDataTable table)
    {
        var enabled = (bool)args.Value!;
        if (enabled)
        {
            User?.DataTables.Add(table);
        }
        else
        {
            User?.DataTables.Remove(table);
        }
    }

    private void OnClosed()
    {
        User = null;
    }

    private async Task SubmitUser()
    {
        // New user
        if (IsNewUser)
        {
            Password ??= string.Empty;
            ConfirmPassword ??= string.Empty;

            if (AuthenticationResolver.AuthenticationMethod == AuthenticationMethod.BuiltIn && Password.Length < 1 || Password.Length > 250)
            {
                Messenger.AddError("Password must be between 1 and 250 characters in length");
                return;
            }

            if (AuthenticationResolver.AuthenticationMethod == AuthenticationMethod.BuiltIn && !Password.Equals(ConfirmPassword))
            {
                Messenger.AddError("The two passwords do not match");
                return;
            }

            try
            {
                ArgumentNullException.ThrowIfNull(User);
                var context = await DbFactory.CreateDbContextAsync();
                var transaction = context.Database.BeginTransaction().GetDbTransaction();

                try
                {
                    // Add user without password
                    context.Users.Add(User);

                    await context.SaveChangesAsync();

                    var connection = context.Database.GetDbConnection();

                    // Update the password hash.
                    await UserService.UpdatePasswordAsync(User.Username, Password, connection, transaction);

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                Password = null;
                ConfirmPassword = null;
                await OnUserSubmit.InvokeAsync(User);
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
                ArgumentNullException.ThrowIfNull(User);
                Context.Attach(User).Property(u => u.Roles).IsModified = true;
                await Context.SaveChangesAsync();
                await OnUserSubmit.InvokeAsync(User);
                await Modal.LetAsync(x => x.HideAsync());
            }
            catch (Exception ex)
            {
                Messenger.AddError("Error updating user", ex.Message);
            }
        }
    }

    public async Task ShowAsync(string? username)
    {
        await Modal.LetAsync(x => x.ShowAsync());
        PreviousUsername = username;
        if (username is null)
        {
            await ResetContext();
            User = new()
            {
                Username = "",
                Roles = new() { Roles.Viewer },
                Jobs = new List<Job>(),
                DataTables = new List<MasterDataTable>()
            };
        }
        else
        {
            await ResetContext();
            User = await Context.Users
                .Include(u => u.Jobs)
                .Include(u => u.DataTables)
                .FirstAsync(user => user.Username == username);
        }
        Jobs = await Context.Jobs
            .Include(j => j.Category)
            .OrderBy(j => j.JobName)
            .ToListAsync();
        DataTables = await Context.MasterDataTables
            .Include(t => t.Category)
            .OrderBy(t => t.DataTableName)
            .ToListAsync();
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
