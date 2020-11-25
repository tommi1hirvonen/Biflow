using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EtlManager.Pages.Shared
{
    public class _JobDetailsNavigationPartialModel : PageModel
    {
        public Job Job { get; set; }
        public IList<Job> Jobs { get; set; }
        public string RedirectPage { get; set; }

        public bool IsEditor { get; set; }

        public bool Subscribed { get; set; }

        [Required]
        [MaxLength(250)]
        public string NewJobName { get; set; }
    }
}