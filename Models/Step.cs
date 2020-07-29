using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ExecutorManager.Models
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
        [Display(Name = "Step Name")]
        public string StepName { get; set; }

        [Required]
        [Display(Name = "Execution Phase")]
        public int ExecutionPhase { get; set; }

        [Required]
        [Display(Name = "Step Type")]
        public string StepType { get; set; }

        [Display(Name = "SQL Statement")]
        public string SqlStatement { get; set; }

        [MaxLength(250)]
        [Display(Name = "Folder Name")]
        public string FolderName { get; set; }

        [MaxLength(250)]
        [Display(Name = "Project Name")]
        public string ProjectName { get; set; }

        [MaxLength(250)]
        [Display(Name = "Package Name")]
        public string PackageName { get; set; }

        [Required]
        [Display(Name = "32 Bit Mode")]
        public bool ExecuteIn32BitMode { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Created")]
        public DateTime CreatedDateTime { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Last Modified")]
        public DateTime LastModifiedDateTime { get; set; }

        [Required]
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; }
    }
}
