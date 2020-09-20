using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EtlManager.Pages.Shared
{
    public class _JobDetailsNavigationPartialModel : PageModel
    {
        public Job Job { get; set; }
        public IList<Job> Jobs { get; set; }
        public string RedirectPage { get; set; }
    }
}