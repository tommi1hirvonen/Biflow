using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record PackageStep : Step
    {
        [Column("ConnectionId")]
        public Guid? ConnectionId { get; set; }

        [MaxLength(128)]
        [Display(Name = "Folder name")]
        public string? PackageFolderName { get; set; }

        [MaxLength(128)]
        [Display(Name = "Project name")]
        public string? PackageProjectName { get; set; }

        [MaxLength(260)]
        [Display(Name = "Package name")]
        public string? PackageName { get; set; }

        [Required]
        [Display(Name = "32 bit mode")]
        public bool ExecuteIn32BitMode { get; set; }

        [Display(Name = "Execute as login")]
        public string? ExecuteAsLogin { get; set; }

        public Connection? Connection { get; set; }

        public IList<PackageParameter> PackageParameters { get; set; } = null!;
    }
}
