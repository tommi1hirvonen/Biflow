using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Biflow.Ui.Shared.Settings;

public partial class UserEditModal : ComponentBase, IDisposable
{
    [Inject] private AuthenticationMethodResolver AuthenticationResolver { get; set; } = null!;
    
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;
        
    [Inject] private DbHelperService DbHelperService { get; set; } = null!;
    
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
                await DbHelperService.AddUserAsync(User, Password);
                
                // Add possible job authorizations.
                var context = DbFactory.CreateDbContext();
                var user = await context.Users
                    .Include(u => u.Jobs)
                    .Include(u => u.DataTables)
                    .FirstAsync(u => u.Username == User.Username);
                var jobs = await context.Jobs.ToListAsync();
                var dataTables = await context.MasterDataTables.ToListAsync();
                user.AuthorizeAllJobs = User.AuthorizeAllJobs;
                user.AuthorizeAllDataTables = User.AuthorizeAllDataTables;
                foreach (var jobToAdd in User.Jobs)
                {
                    var job = jobs.FirstOrDefault(j => j.JobId == jobToAdd.JobId);
                    if (job is not null)
                        user.Jobs.Add(job);
                }
                foreach (var tableToAdd in User.DataTables)
                {
                    var table = dataTables.FirstOrDefault(t => t.DataTableId == tableToAdd.DataTableId);
                    if (table is not null)
                        user.DataTables.Add(table);
                }

                await context.SaveChangesAsync();

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
                Role = "Viewer",
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
