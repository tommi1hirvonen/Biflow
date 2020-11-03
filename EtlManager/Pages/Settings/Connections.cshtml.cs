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
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages.Settings
{
    [Authorize(Policy = "RequireAdmin")]
    public class ConnectionsModel : PageModel
    {
        private readonly EtlManagerContext context;
        private readonly IConfiguration configuration;

        public ConnectionsModel(EtlManagerContext context, IConfiguration configuration)
        {
            this.context = context;
            this.configuration = configuration;
        }

        public IList<Connection> Connections { get; set; }

        public Connection NewConnection { get; set; }

        public Connection EditConnection { get;  set; }

        public void OnGet()
        {
            Connections = context.Connections.OrderBy(conn => conn.ConnectionName).ToList();
        }

        public async Task<IActionResult> OnPostCreate([Bind("ConnectionId", "ConnectionName", "ConnectionString", "IsSensitive")] Connection NewConnection)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage("./Connections");
            }

            string encryptionPassword = configuration.GetValue<string>("EncryptionPassword");

            await context.Database.ExecuteSqlRawAsync("etlmanager.ConnectionAdd {0}, {1}, {2}, {3}", parameters: new string[]
            {
                NewConnection.ConnectionName,
                NewConnection.ConnectionString,
                NewConnection.IsSensitive ? "1" : "0",
                encryptionPassword
            });

            return RedirectToPage("./Connections");
        }

        public async Task<IActionResult> OnPostEdit([Bind("ConnectionId", "ConnectionName", "ConnectionString", "IsSensitive")] Connection EditConnection)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage("./Connections");
            }

            string encryptionPassword = configuration.GetValue<string>("EncryptionPassword");

            await context.Database.ExecuteSqlRawAsync("etlmanager.ConnectionUpdate {0}, {1}, {2}, {3}, {4}", parameters: new string[]
            {
                EditConnection.ConnectionId.ToString(),
                EditConnection.ConnectionName,
                EditConnection.ConnectionString,
                EditConnection.IsSensitive ? "1" : "0",
                encryptionPassword
            });

            return RedirectToPage("./Connections");
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            if (id == null) return NotFound();

            Connection connection = await context.Connections.FindAsync(id);

            if (connection == null) return NotFound();

            context.Connections.Remove(connection);
            await context.SaveChangesAsync();

            return RedirectToPage("./Connections");
        }
    }
}
