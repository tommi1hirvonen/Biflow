using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Settings
{
    public class ConnectionsModel : PageModel
    {
        private readonly EtlManagerContext context;
        private readonly IAuthorizationService authorizationService;

        public ConnectionsModel(EtlManagerContext context, IAuthorizationService authorizationService)
        {
            this.context = context;
            this.authorizationService = authorizationService;
        }

        public IList<Connection> Connections { get; set; }

        public Connection NewConnection { get; set; }

        public Connection EditConnection { get;  set; }

        public void OnGet()
        {
            Connections = context.Connections.OrderBy(conn => conn.ConnectionName).ToList();
        }

        public async Task<IActionResult> OnPostCreate([Bind("ConnectionId", "ConnectionName", "ConnectionString")] Connection NewConnection)
        {
            var authorized = await authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return RedirectToPage("./Connections");
            }

            context.Connections.Add(NewConnection);
            await context.SaveChangesAsync();
            return RedirectToPage("./Connections");
        }

        public async Task<IActionResult> OnPostEdit([Bind("ConnectionId", "ConnectionName", "ConnectionString")] Connection EditConnection)
        {
            var authorized = await authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return RedirectToPage("./Connections");
            }

            context.Attach(EditConnection).State = EntityState.Modified;
            await context.SaveChangesAsync();

            return RedirectToPage("./Connections");
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            var authorized = await authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (id == null) return NotFound();

            Connection connection = await context.Connections.FindAsync(id);

            if (connection == null) return NotFound();

            context.Connections.Remove(connection);
            await context.SaveChangesAsync();

            return RedirectToPage("./Connections");
        }
    }
}
