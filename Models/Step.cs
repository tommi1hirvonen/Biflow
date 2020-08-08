using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Models
{
    public class Step
    {
        [Key]
        [Required]
        public Guid StepId { get; set; }

        [Required]
        public Guid JobId { get; set; }

        public Job Job { get; set; }

        [Required]
        [MaxLength(250)]
        [Display(Name = "Step name")]
        public string StepName { get; set; }

        [Required]
        [Display(Name = "Execution phase")]
        public int ExecutionPhase { get; set; }

        [Required]
        [Display(Name = "Step type")]
        public string StepType { get; set; }

        [Display(Name = "SQL statement")]
        public string SqlStatement { get; set; }

        [MaxLength(250)]
        [Display(Name = "Folder name")]
        public string FolderName { get; set; }

        [MaxLength(250)]
        [Display(Name = "Project name")]
        public string ProjectName { get; set; }

        [MaxLength(250)]
        [Display(Name = "Package name")]
        public string PackageName { get; set; }

        [Required]
        [Display(Name = "32 bit mode")]
        public bool ExecuteIn32BitMode { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Created")]
        public DateTime CreatedDateTime { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Last modified")]
        public DateTime LastModifiedDateTime { get; set; }

        [Required]
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; }

        public ICollection<Dependency> Dependencies { get; set; }

        public IList<Parameter> Parameters { get; set; }
    }
}
