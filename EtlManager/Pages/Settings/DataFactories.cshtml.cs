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

namespace EtlManager.Pages.Settings
{
    [Authorize(Policy = "RequireAdmin")]
    public class DataFactoriesModel : PageModel
    {
        private readonly EtlManagerContext context;
        private readonly IAuthorizationService authorizationService;

        public DataFactoriesModel(EtlManagerContext context, IAuthorizationService authorizationService)
        {
            this.context = context;
            this.authorizationService = authorizationService;
        }

        public IList<DataFactory> DataFactories { get; set; }

        public DataFactory NewDataFactory { get; set; }

        public DataFactory EditDataFactory { get; set; }

        public void OnGet()
        {
            DataFactories = context.DataFactories.OrderBy(df => df.DataFactoryName).ToList();
        }

        public async Task<IActionResult> OnPostCreate([Bind("DataFactoryId", "DataFactoryName", "TenantId", "SubscriptionId",
            "ClientId", "ClientSecret", "ResourceGroupName", "ResourceName")] DataFactory NewDataFactory)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage("./DataFactories");
            }

            context.DataFactories.Add(NewDataFactory);
            await context.SaveChangesAsync();
            return RedirectToPage("./DataFactories");
        }

        public async Task<IActionResult> OnPostEdit([Bind("DataFactoryId", "DataFactoryName", "TenantId", "SubscriptionId",
            "ClientId", "ClientSecret", "ResourceGroupName", "ResourceName")] DataFactory EditDataFactory)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage("./DataFactories");
            }

            context.Attach(EditDataFactory).State = EntityState.Modified;
            await context.SaveChangesAsync();

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