using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class ExeStep : Step
    {
        [Display(Name = "File path")]
        public string? ExeFileName { get; set; }

        [Display(Name = "Arguments")]
        public string? ExeArguments { get; set; }

        [Display(Name = "Working directory")]
        public string? ExeWorkingDirectory { get; set; }

        [Display(Name = "Success exit code")]
        public int? ExeSuccessExitCode { get; set; }
    }
}
