using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class PackageStepExecution : ParameterizedStepExecution
    {
        public PackageStepExecution(string stepName) : base(stepName)
        {
        }

        [MaxLength(128)]
        [Display(Name = "Folder name")]
        public string? PackageFolderName { get; set; }

        [MaxLength(128)]
        [Display(Name = "Project name")]
        public string? PackageProjectName { get; set; }

        [MaxLength(260)]
        [Display(Name = "Package name")]
        public string? PackageName { get; set; }

        [Display(Name = "32 bit mode")]
        public bool ExecuteIn32BitMode { get; set; }

        [Display(Name = "Execute as login")]
        public string? ExecuteAsLogin { get; set; }

        [NotMapped]
        public string? PackagePath => PackageFolderName + "/" + PackageProjectName + "/" + PackageName;
    }
}
