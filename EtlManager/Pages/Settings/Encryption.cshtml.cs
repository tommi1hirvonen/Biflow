using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages.Settings
{
    public class EncryptionModel : PageModel
    {
        private readonly IConfiguration configuration;

        public EncryptionModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [BindProperty]
        [Required]
        [MaxLength(128)]
        [MinLength(1)]
        [DataType(DataType.Password)]
        [Display(Name = "New encryption key")]
        public string EncryptionKey { get; set; }

        [BindProperty]
        [Required]
        [MaxLength(128)]
        [MinLength(1)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new encryption key")]
        public string ConfirmEncryptionKey { get; set; }

        public bool Success { get; set; } = false;

        public bool IsEncryptionKeySet { get; set; } = false;

        private void InitializeModel()
        {
            IsEncryptionKeySet = Utility.IsEncryptionKeySet(configuration);
        }

        public void OnGet()
        {
            InitializeModel();
        }

        public IActionResult OnPostChangeEncryptionKey()
        {
            if (ModelState.ContainsKey("MatchError")) ModelState["MatchError"].Errors.Clear();
            if (ModelState.IsValid)
            {
                if (EncryptionKey.Equals(ConfirmEncryptionKey))
                {
                    string oldEncryptionKey = Utility.GetEncryptionKey(configuration);

                    Utility.SetEncryptionKey(configuration, oldEncryptionKey, EncryptionKey);

                    Success = true;
                }
                else
                {
                    ModelState.AddModelError("MatchError", "The two keys do not match");
                }
            }

            InitializeModel();
            return Page();
        }
    }
}
