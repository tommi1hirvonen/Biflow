using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public class DataFactoriesModel : PageModel
    {
        private readonly EtlManagerContext context;
        private readonly IConfiguration configuration;

        public DataFactoriesModel(EtlManagerContext context, IConfiguration configuration)
        {
            this.context = context;
            this.configuration = configuration;
        }

        public bool IsEncryptionKeySet { get; set; } = false;

        public IList<DataFactory> DataFactories { get; set; }

        public DataFactory NewDataFactory { get; set; }

        public DataFactory EditDataFactory { get; set; }

        public void OnGet()
        {
            IsEncryptionKeySet = Utility.IsEncryptionKeySet(configuration);
            DataFactories = context.DataFactories.OrderBy(df => df.DataFactoryName).ToList();
        }

        public async Task<IActionResult> OnPostCreate([Bind("DataFactoryId", "DataFactoryName", "TenantId", "SubscriptionId",
            "ClientId", "ClientSecret", "ResourceGroupName", "ResourceName")] DataFactory NewDataFactory)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage("./DataFactories");
            }

            string encryptionPassword = Utility.GetEncryptionKey(configuration);

            await context.Database.ExecuteSqlRawAsync("etlmanager.DataFactoryAdd {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", parameters: new string[]
            {
                NewDataFactory.DataFactoryName,
                NewDataFactory.TenantId,
                NewDataFactory.SubscriptionId,
                NewDataFactory.ClientId,
                NewDataFactory.ClientSecret,
                NewDataFactory.ResourceGroupName,
                NewDataFactory.ResourceName,
                encryptionPassword
            });
            return RedirectToPage("./DataFactories");
        }

        public async Task<IActionResult> OnPostEdit([Bind("DataFactoryId", "DataFactoryName", "TenantId", "SubscriptionId",
            "ClientId", "ClientSecret", "ResourceGroupName", "ResourceName")] DataFactory EditDataFactory)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage("./DataFactories");
            }

            string encryptionPassword = Utility.GetEncryptionKey(configuration);

            await context.Database.ExecuteSqlRawAsync("etlmanager.DataFactoryUpdate {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", parameters: new string[]
            {
                EditDataFactory.DataFactoryId.ToString(),
                EditDataFactory.DataFactoryName,
                EditDataFactory.TenantId,
                EditDataFactory.SubscriptionId,
                EditDataFactory.ClientId,
                EditDataFactory.ClientSecret,
                EditDataFactory.ResourceGroupName,
                EditDataFactory.ResourceName,
                encryptionPassword
            });

            return RedirectToPage("./DataFactories");
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            if (id == null) return NotFound();

            DataFactory dataFactory = await context.DataFactories.FindAsync(id);

            if (dataFactory == null) return NotFound();

            context.DataFactories.Remove(dataFactory);
            await context.SaveChangesAsync();

            return RedirectToPage("./DataFactories");
        }
    }
}